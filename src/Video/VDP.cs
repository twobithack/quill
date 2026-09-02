using System;
using System.Runtime.CompilerServices;

using Quill.Common.Extensions;
using Quill.Common.Interfaces;
using Quill.Video.Definitions;

namespace Quill.Video;

public sealed partial class VDP
{
  #region Constants
  public const int VRAM_SIZE = 0x4000;
  public const int CRAM_SIZE = 0x20;
  public const int REGISTER_COUNT = 11;
  
  private const int HORIZONTAL_RESOLUTION = 256;
  private const int VERTICAL_RESOLUTION = 240;
  private const int BACKGROUND_COLUMNS = 32;
  private const int BACKGROUND_ROWS = 28;
  private const byte VCOUNTER_ACTIVE = 191;
  private const byte VCOUNTER_JUMP_FROM = 218;
  private const byte VCOUNTER_JUMP_TO = 213;
  private const int HSCROLL_INHIBIT_END_ROW = 1;
  private const int VSCROLL_INHIBIT_START_COLUMN = 24;
  private const int HCOUNT_PER_CYCLE = 3;
  private const int HCOUNTER_MAX = 684;
  private const int VCOUNTER_MAX = byte.MaxValue;
  
  private const byte SPRITE_TERMINATOR = 0xD0;
  private const byte TRANSPARENT_COLOR_INDEX = 0x00;
  private const int TILE_SIZE = 8;
  private const int TILE_SHIFT = 3;
  #endregion

  public VDP(IVideoSink framebuffer)
  {
    _framebuffer = framebuffer;
    _vram = new byte[VRAM_SIZE];
    _palette = new int[CRAM_SIZE];
    _registers = new byte[REGISTER_COUNT];
    _scanlinePixels = new int[HORIZONTAL_RESOLUTION];
    _spriteMask = new bool[HORIZONTAL_RESOLUTION];
  }

  #region Methods
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public byte ReadStatus()
  {
    _controlWriteLatch = false;
    _lineInterruptPending = false;
    IRQ = false;

    var status = (byte)_status;
    if (DisplayMode4)
      _status &= ~Status.Flags;
    else
      _status &= ~(Status.SpriteCollision | Status.VBlank);
    return status;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void WriteControl(byte controlByte)
  {
    if (!_controlWriteLatch)
    {
      _addressRegister &= 0b_1111_1111_0000_0000;
      _addressRegister |= controlByte;
      _controlWriteLatch = true;
      return;
    }
    
    _addressRegister &= 0b_0000_0000_1111_1111;
    _addressRegister |= (ushort)((controlByte & 0b_0011_1111) << 8);
    _controlCode = (ControlCode)(controlByte >> 6);
    _controlWriteLatch = false;

    if (_controlCode == ControlCode.ReadVRAM)
    {
      _dataBuffer = _vram[_addressRegister];
      IncrementAddressRegister();
    }
    else if (_controlCode == ControlCode.WriteRegister)
    {
      var register = _addressRegister.HighByte().LowNibble();
      if (register >= REGISTER_COUNT)
        return;

      WriteRegister(register, _addressRegister.LowByte());
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public byte ReadData()
  {
    _controlWriteLatch = false;

    var data = _dataBuffer;
    _dataBuffer = _vram[_addressRegister];
    IncrementAddressRegister();
    return data;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void WriteData(byte value)
  {
    _controlWriteLatch = false;

    if (_controlCode == ControlCode.WriteCRAM)
    {
      var colorIndex = _addressRegister & 0b_0001_1111;
      _palette[colorIndex] = Color.ToRGBA(value);
    }
    else
      _vram[_addressRegister] = value;

    _dataBuffer = value;
    IncrementAddressRegister();
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void Step(int cycles)
  {
    _hCounter += (ushort)(cycles * HCOUNT_PER_CYCLE);

    if (_hCounter < HCOUNTER_MAX)
      return;
      
    _hCounter -= HCOUNTER_MAX;
    RenderScanline();
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool PollFrameCompletion()
  {
    if (!_frameCompleted)
      return false;
      
    _frameCompleted = false;
    return true;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public byte[] ReadFramebuffer() => _framebuffer.ReadFrame();

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RenderScanline()
  {
    IncrementScanline();
    UpdateInterrupts();

    if (_vCounter >= VERTICAL_RESOLUTION)
      return;

    if (DisplayMode4)
      RasterizeMode4Scanline();
    else
      RasterizeLegacyScanline();

    CommitScanline();
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void IncrementScanline()
  {
    if (_vCounter == VCOUNTER_ACTIVE + 1)
    {
      VBlank = true;
    }
    else if (_vCounter == VCOUNTER_JUMP_FROM)
    {
      if (!_vCounterJumped)
      {
        _vCounter = VCOUNTER_JUMP_TO;
        _vCounterJumped = true;
        return;
      }
    }
    else if (_vCounter == VCOUNTER_MAX)
    {
      _framebuffer.PresentFrame();
      _vScroll = _registers[0x9];
      _vCounterJumped = false;
      _frameCompleted = true;
    }
    
    _vCounter++;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void UpdateInterrupts()
  {
    if (_vCounter > VCOUNTER_ACTIVE + 1)
    {
      _lineCounter = _registers[0xA];
    }
    else if (_lineCounter == 0)
    {
      _lineCounter = _registers[0xA];
      _lineInterruptPending = LineInterruptEnabled;
    }
    else
      _lineCounter--;

    IRQ = VBlankInterruptAsserted || LineInterruptAsserted;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeMode4Scanline()
  {
    if (!DisplayEnabled || _vCounter > VCOUNTER_ACTIVE)
    {
      BlankMode4Scanline();
    }
    else
    {
      RasterizeMode4Sprites();
      RasterizeMode4Background();
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeMode4Sprites()
  {
    var spriteHeight = TILE_SIZE;
    if (StretchSprites || MagnifySprites)
      spriteHeight <<= 1;

    var spritesOnScanline = 0;
    for (int spriteIndex = 0; spriteIndex < 64; spriteIndex++)
    {
      int spriteY = _vram[SpriteAttributeTableBaseAddress + spriteIndex];
      if (spriteY == SPRITE_TERMINATOR)
        return;

      spriteY++;
      if (spriteY >= SPRITE_TERMINATOR)
        spriteY -= 0x100;

      if (spriteY > _vCounter ||
          spriteY + spriteHeight <= _vCounter)
        continue;

      spritesOnScanline++;
      if (spritesOnScanline > 8)
        SpriteOverflow = true;

      var attributeOffset = 0x80 + (spriteIndex << 1);
      int spriteX = _vram[SpriteAttributeTableBaseAddress + attributeOffset];
      int patternIndex = _vram[SpriteAttributeTableBaseAddress + attributeOffset + 1];

      if (ShiftSprites)
        spriteX -= TILE_SIZE;

      if (StretchSprites && spriteY <= _vCounter + TILE_SIZE)
        patternIndex &= 0b_1111_1111_1111_1110;

      var patternRowOffset = (_vCounter - spriteY) << 2;
      var patternOffset = patternIndex << 5;
      var patternAddress = SpritePatternGeneratorTableBaseAddress
                         + patternRowOffset
                         + patternOffset;
      var patternRow = ReadPatternRow(patternAddress);

      var spriteRight = spriteX + TILE_SIZE;
      for (byte patternBit = TILE_SIZE - 1; spriteX < spriteRight; spriteX++, patternBit--)
      {
        if (spriteX >= HORIZONTAL_RESOLUTION)
          break;

        if (spriteX < 0)
          continue;

        if (BlankLeftColumn && spriteX < TILE_SIZE)
          continue;

        var colorIndex = patternRow.GetColorIndex(patternBit);
        if (colorIndex == TRANSPARENT_COLOR_INDEX)
          continue;
        colorIndex += 16;

        if (_spriteMask[spriteX])
        {
          SpriteCollision = true;
          continue;
        }

        SetMode4SpritePixel(spriteX, colorIndex);
      }
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeMode4Background()
  {
    var applyHorizontalScroll = !HScrollInhibit ||
                                (_vCounter >> TILE_SHIFT) > HSCROLL_INHIBIT_END_ROW;

    for (int screenColumn = 0; screenColumn < BACKGROUND_COLUMNS; screenColumn++)
    {
      ushort nameTableY = _vCounter;
      if (!VScrollInhibit ||
          screenColumn < VSCROLL_INHIBIT_START_COLUMN)
        nameTableY += _vScroll;

      var nameTableRow = nameTableY >> TILE_SHIFT;
      if (nameTableRow >= BACKGROUND_ROWS)
        nameTableRow -= BACKGROUND_ROWS;

      var nameTableColumn = screenColumn;
      if (applyHorizontalScroll)
        nameTableColumn += BACKGROUND_COLUMNS - (HScroll >> TILE_SHIFT);
      nameTableColumn &= BACKGROUND_COLUMNS - 1;

      var nameTableEntryAddress = NameTableBaseAddress
                                + (nameTableRow    << 6)
                                + (nameTableColumn << 1);

      var nameTableEntry = ReadNameTableEntry(nameTableEntryAddress);
      var patternRowIndex = nameTableEntry.VerticalFlip
                          ? 7 - (nameTableY & (TILE_SIZE - 1))
                          : nameTableY & (TILE_SIZE - 1);

      var patternAddress = (nameTableEntry.PatternIndex << 5)
                         + (patternRowIndex << 2);
      var patternRow = ReadPatternRow(patternAddress);

      for (int patternColumn = 0; patternColumn < TILE_SIZE; patternColumn++)
      {
        var screenColumnOffset = nameTableEntry.HorizontalFlip
                               ? 7 - patternColumn
                               : patternColumn;

        var screenX = (nameTableColumn << TILE_SHIFT)
                    + screenColumnOffset;
        if (applyHorizontalScroll)
          screenX += HScroll;
        screenX &= HORIZONTAL_RESOLUTION - 1;

        if (BlankLeftColumn && screenX < TILE_SIZE)
        {
          SetMode4BackgroundPixel(screenX, BackdropColorIndex);
          continue;
        }

        var colorIndex = patternRow.GetColorIndex(7 - patternColumn);

        if (_spriteMask[screenX] &&
            (!nameTableEntry.HighPriority || colorIndex == TRANSPARENT_COLOR_INDEX))
          continue;

        if (nameTableEntry.UseSecondPalette)
          colorIndex += 16;

        SetMode4BackgroundPixel(screenX, colorIndex);
      }
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private PatternRow ReadPatternRow(int patternAddress) => new(_vram[patternAddress],
                                                               _vram[patternAddress + 1],
                                                               _vram[patternAddress + 2],
                                                               _vram[patternAddress + 3]);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private NameTableEntry ReadNameTableEntry(int address)
  {
    var data = _vram[address + 1].Concat(_vram[address]);
    return new NameTableEntry(data);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void BlankMode4Scanline()
  {
    var fillColor = _palette[BackdropColorIndex];
    Array.Fill(_scanlinePixels, fillColor);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SetMode4SpritePixel(int x, int paletteIndex)
  {
    _scanlinePixels[x] = _palette[paletteIndex];
    _spriteMask[x] = true;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SetMode4BackgroundPixel(int x, int paletteIndex) => _scanlinePixels[x] = _palette[paletteIndex];

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void CommitScanline()
  {
    _framebuffer.BlitScanline(_vCounter, _scanlinePixels);
    Array.Clear(_spriteMask);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void UpdateDisplayMode()
  {
    _displayMode = DisplayMode.None;
    if (TestRegisterBit(0x1, 4))
      _displayMode |= DisplayMode.Mode_1;
    if (TestRegisterBit(0x0, 1))
      _displayMode |= DisplayMode.Mode_2;
    if (TestRegisterBit(0x1, 3))
      _displayMode |= DisplayMode.Mode_3;
    if (TestRegisterBit(0x0, 2))
      _displayMode |= DisplayMode.Mode_4;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void IncrementAddressRegister() => _addressRegister = (ushort)((_addressRegister + 1) & (VRAM_SIZE - 1));

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private bool TestRegisterBit(byte register, byte bit) => _registers[register].TestBit(bit);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void WriteRegister(byte register, byte value)
  {
    _registers[register] = value;

    if (register == 0x0)
    {
      if (IRQ && !LineInterruptEnabled)
        IRQ = VBlankInterruptAsserted;
      UpdateDisplayMode();
    }
    else if (register == 0x1)
    {
      if (IRQ && !VBlankInterruptEnabled)
        IRQ = LineInterruptAsserted;
      UpdateDisplayMode();
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private bool GetFlag(Status flag) => (_status & flag) != 0;
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SetFlag(Status flag, bool value) => _status = value
                                                 ? _status | flag
                                                 : _status & ~flag;
  #endregion
}