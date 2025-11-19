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
  private const int HSCROLL_LIMIT = 1;
  private const int VSCROLL_LIMIT = 24;
  private const int HCOUNT_PER_CYCLE = 3;
  private const int HCOUNTER_MAX = 684;
  private const int VCOUNTER_MAX = byte.MaxValue;
  
  private const byte DISABLE_SPRITES = 0xD0;
  private const byte TRANSPARENT = 0x00;
  private const int TILE_SIZE = 8;
  private const int TILE_SHIFT = 3;
  #endregion

  public VDP(IVideoSink framebuffer)
  {
    _framebuffer = framebuffer;
    _vram = new byte[VRAM_SIZE];
    _palette = new int[CRAM_SIZE];
    _registers = new byte[REGISTER_COUNT];
    _scanline = new int[HORIZONTAL_RESOLUTION];
    _spriteMask = new bool[HORIZONTAL_RESOLUTION];
  }

  #region Methods
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public byte ReadStatus()
  {
    _controlWriteLatch = false;
    _hLineInterruptPending = false;
    IRQ = false;

    var value = (byte)_status;
    if (DisplayMode4)
      _status &= ~Status.All;
    else
      _status &= ~(Status.Collision | Status.VBlank);
    return value;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void WriteControl(byte value)
  {
    if (!_controlWriteLatch)
    {
      _addressBus &= 0b_1111_1111_0000_0000;
      _addressBus |= value;
      _controlWriteLatch = true;
      return;
    }
    
    _addressBus &= 0b_0000_0000_1111_1111;
    _addressBus |= (ushort)((value & 0b_0011_1111) << 8);
    _controlCode = (ControlCode)(value >> 6);
    _controlWriteLatch = false;

    if (_controlCode == ControlCode.ReadVRAM)
    {
      _dataBuffer = _vram[_addressBus];
      IncrementAddress();
    }
    else if (_controlCode == ControlCode.WriteRegister)
    {
      var register = _addressBus.HighByte().LowNibble();
      if (register >= REGISTER_COUNT)
        return;

      WriteRegister(register, _addressBus.LowByte());
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public byte ReadData()
  {
    _controlWriteLatch = false;

    var data = _dataBuffer;
    _dataBuffer = _vram[_addressBus];
    IncrementAddress();
    return data;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void WriteData(byte value)
  {
    _controlWriteLatch = false;

    if (_controlCode == ControlCode.WriteCRAM)
    {
      var index = _addressBus & 0b_0001_1111;
      _palette[index] = Color.ToRGBA(value);
    }
    else
      _vram[_addressBus] = value;

    _dataBuffer = value;
    IncrementAddress();
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
  public bool FrameCompleted()
  {
    if (!_vBlankCompleted)
      return false;
      
    _vBlankCompleted = false;
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
      RasterizeScanline();
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
      _vBlankCompleted = true;
    }
    
    _vCounter++;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void UpdateInterrupts()
  {
    if (_vCounter > VCOUNTER_ACTIVE + 1)
    {
      _hLineCounter = _registers[0xA];
    }
    else if (_hLineCounter == 0)
    {
      _hLineCounter = _registers[0xA];
      _hLineInterruptPending = HLineInterruptEnabled;
    }
    else
      _hLineCounter--;

    IRQ = VSyncPending || HLinePending;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeScanline()
  {
    if (!DisplayEnabled || _vCounter > VCOUNTER_ACTIVE)
    {
      BlankScanline();
    }
    else
    {
      RasterizeSprites();
      RasterizeBackground();
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeSprites()
  {
    var spriteHeight = TILE_SIZE;
    if (StretchSprites || ZoomSprites)
      spriteHeight <<= 1;

    var spriteCount = 0;
    for (int sprite = 0; sprite < 64; sprite++)
    {
      ushort y = _vram[SpriteAttributeTableAddress + sprite];
      if (y == DISABLE_SPRITES)
        return;

      y++;
      if (y >= DISABLE_SPRITES)
        y -= 0x100;

      if (y > _vCounter ||
          y + spriteHeight <= _vCounter)
        continue;

      spriteCount++;
      if (spriteCount > 8)
        SpriteOverflow = true;

      var offset = 0x80 + (sprite << 1);
      int x = _vram[SpriteAttributeTableAddress + offset];
      int patternIndex = _vram[SpriteAttributeTableAddress + offset + 1];

      if (SpriteShift)
        x -= TILE_SIZE;

      if (StretchSprites && y <= _vCounter + TILE_SIZE)
        patternIndex &= 0b_1111_1111_1111_1110;

      var rowOffset = (_vCounter - y) << 2;
      var patternOffset = patternIndex << 5;
      var patternAddress = SpritePatternTableAddress 
                         + rowOffset 
                         + patternOffset;
      var patternData = GetPatternData(patternAddress);

      var spriteEnd = x + TILE_SIZE;
      for (byte i = TILE_SIZE - 1; x < spriteEnd; x++, i--)
      {
        if (x >= HORIZONTAL_RESOLUTION)
          break;

        if (BlankLeftColumn && x < TILE_SIZE)
          continue;

        var paletteIndex = patternData.GetPaletteIndex(i);
        if (paletteIndex == TRANSPARENT)
          continue;
        paletteIndex += 16;

        if (_spriteMask[x])
        {
          SpriteCollision = true;
          continue;
        }

        SetSpritePixel(x, paletteIndex);
      }
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeBackground()
  {
    var allowHScroll = !HScrollInhibit ||
                       (_vCounter >> TILE_SHIFT) > HSCROLL_LIMIT;

    for (int backgroundColumn = 0; backgroundColumn < BACKGROUND_COLUMNS; backgroundColumn++)
    {
      ushort tilemapY = _vCounter;
      if (!VScrollInhibit ||
          backgroundColumn < VSCROLL_LIMIT)
        tilemapY += _vScroll;

      var tilemapRow = tilemapY >> TILE_SHIFT;
      if (tilemapRow >= BACKGROUND_ROWS) 
        tilemapRow -= BACKGROUND_ROWS;
                    
      var tilemapColumn = backgroundColumn;
      if (allowHScroll)
        tilemapColumn += BACKGROUND_COLUMNS - (HScroll >> TILE_SHIFT);
      tilemapColumn &= BACKGROUND_COLUMNS - 1;

      var tileAddress = NameTableAddress 
                      + (tilemapRow    << 6)
                      + (tilemapColumn << 1);

      var tileData = GetTileData(tileAddress);
      var tileOffset = tileData.VerticalFlip
                     ? 7 - (tilemapY & (TILE_SIZE - 1))
                     : tilemapY & (TILE_SIZE - 1);

      var patternAddress = (tileData.PatternIndex << 5) 
                         + (tileOffset << 2);
      var patternData = GetPatternData(patternAddress);

      for (int i = 0; i < TILE_SIZE; i++)
      {
        var columnOffset = tileData.HorizontalFlip
                         ? 7 - i
                         : i;

        var x = (tilemapColumn << TILE_SHIFT) 
              + columnOffset;
        if (allowHScroll)
          x += HScroll;
        x &= HORIZONTAL_RESOLUTION - 1;

        if (BlankLeftColumn && x < TILE_SIZE)
        {
          SetBackgroundPixel(x, BlankColor);
          continue;
        }

        var paletteIndex = patternData.GetPaletteIndex(7 - i);

        if (_spriteMask[x] &&
            (!tileData.HighPriority || paletteIndex == TRANSPARENT))
          continue;

        if (tileData.UseSpritePalette)
          paletteIndex += 16;

        SetBackgroundPixel(x, paletteIndex);
      }
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private Pattern GetPatternData(int patternAddress) => new(_vram[patternAddress],
                                                            _vram[patternAddress + 1],
                                                            _vram[patternAddress + 2],
                                                            _vram[patternAddress + 3]);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private Tile GetTileData(int tileAddress)
  {
    var data = _vram[tileAddress + 1].Concat(_vram[tileAddress]);
    return new Tile(data);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void BlankScanline()
  {
    var fillColor = _palette[BlankColor];
    Array.Fill(_scanline, fillColor);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SetSpritePixel(int x, int paletteIndex)
  {
    _scanline[x] = _palette[paletteIndex];
    _spriteMask[x] = true;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SetBackgroundPixel(int x, int paletteIndex) => _scanline[x] = _palette[paletteIndex];

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void CommitScanline()
  {
    _framebuffer.BlitScanline(_vCounter, _scanline);
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
  private void IncrementAddress() => _addressBus = (ushort)((_addressBus + 1) & (VRAM_SIZE - 1));

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private bool TestRegisterBit(byte register, byte bit) => _registers[register].TestBit(bit);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void WriteRegister(byte register, byte value)
  {
    _registers[register] = value;

    if (register == 0x0)
    {
      if (IRQ && !HLineInterruptEnabled)
        IRQ = VSyncPending;
      UpdateDisplayMode();
    }
    else if (register == 0x1)
    {
      if (IRQ && !VBlankInterruptEnabled)
        IRQ = HLinePending;
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