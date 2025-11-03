using System;
using System.Linq;
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
  public byte ReadStatus()
  {
    _controlWritePending = false;
    _hLineInterruptPending = false;
    IRQ = false;

    var value = (byte)_status;
    if (DisplayMode4)
      _status &= ~Status.All;
    else
      _status &= ~(Status.Collision | Status.VBlank);
    return value;
  }

  public void WriteControl(byte value)
  {
    if (!_controlWritePending)
    {
      _addressBus &= 0b_1111_1111_0000_0000;
      _addressBus |= value;
      _controlWritePending = true;
      return;
    }
    
    _addressBus &= 0b_0000_0000_1111_1111;
    _addressBus |= (ushort)((value & 0b_0011_1111) << 8);
    _controlCode = (ControlCode)(value >> 6);
    _controlWritePending = false;

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

  public byte ReadData()
  {
    _controlWritePending = false;

    var data = _dataBuffer;
    _dataBuffer = _vram[_addressBus];
    IncrementAddress();
    return data;
  }

  public void WriteData(byte value)
  {
    _controlWritePending = false;

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

  public void Step(int cycles)
  {
    _hCounter += (ushort)(cycles * HCOUNT_PER_CYCLE);

    if (_hCounter < HCOUNTER_MAX)
      return;
      
    _hCounter -= HCOUNTER_MAX;
    RenderScanline();
  }

  public bool FrameCompleted()
  {
    if (!_vBlankCompleted)
      return false;
      
    _vBlankCompleted = false;
    return true;
  }

  public byte[] ReadFramebuffer() => _framebuffer.ReadFrame();

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RenderScanline()
  {
    IncrementScanline();
    UpdateInterrupts();
    RasterizeScanline();
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
    if (_vCounter >= VERTICAL_RESOLUTION)
      return;

    if (!DisplayEnabled || _vCounter > VCOUNTER_ACTIVE)
    {
      BlankScanline();
      return;
    }

    if (DisplayMode4)
    {
      RasterizeSprites();
      RasterizeBackground();
    }
    else
    {
      RasterizeLegacySprites();
      RasterizeLegacyBackground();
    }

    CommitScanline();
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void BlankScanline()
  {
    if (DisplayMode4)
    {
      for (int x = 0; x < HORIZONTAL_RESOLUTION; x++)
        SetPixel(x, BlankColor, false);
    }
    else
    {
      for (int x = 0; x < HORIZONTAL_RESOLUTION; x++)
        SetLegacyPixel(x, LegacyBlankColor, false);
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeSprites()
  {
    var spriteHeight = TILE_SIZE;
    if (StretchSprites || ZoomSprites)
      spriteHeight *= 2;

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

      var offset = 0x80 + (sprite * 2);
      int x = _vram[SpriteAttributeTableAddress + offset];
      int patternIndex = _vram[SpriteAttributeTableAddress + offset + 1];

      if (SpriteShift)
        x -= TILE_SIZE;

      if (StretchSprites && y <= _vCounter + TILE_SIZE)
        patternIndex &= 0b_1111_1111_1111_1110;

      var rowOffset = (_vCounter - y) << 2;
      var patternOffset = patternIndex << 5;
      var patternAddress = SpritePatternTableAddress + rowOffset + patternOffset;
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

        SetPixel(x, paletteIndex, true);
      }
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeBackground()
  {
    var allowHScroll = !HScrollInhibit ||
                       _vCounter / TILE_SIZE > HSCROLL_LIMIT;

    for (int backgroundColumn = 0; backgroundColumn < BACKGROUND_COLUMNS; backgroundColumn++)
    {
      ushort tilemapY = _vCounter;
      if (!VScrollInhibit ||
          backgroundColumn < VSCROLL_LIMIT)
        tilemapY += _vScroll;

      var tilemapRow = tilemapY / TILE_SIZE;
      if (tilemapRow >= BACKGROUND_ROWS) 
        tilemapRow -= BACKGROUND_ROWS;
                    
      var tilemapColumn = backgroundColumn;
      if (allowHScroll)
        tilemapColumn += BACKGROUND_COLUMNS - (HScroll / TILE_SIZE);
      tilemapColumn %= BACKGROUND_COLUMNS;

      var tileAddress = NameTableAddress + 
                        (tilemapRow * BACKGROUND_COLUMNS * 2) + 
                        (tilemapColumn * 2);

      var tileData = GetTileData(tileAddress);
      var tileOffset = tileData.VerticalFlip
                     ? 7 - (tilemapY % TILE_SIZE)
                     : tilemapY % TILE_SIZE;

      var patternAddress = (tileData.PatternIndex * 32) + (tileOffset * 4);
      var patternData = GetPatternData(patternAddress);

      for (int i = 0; i < TILE_SIZE; i++)
      {
        var columnOffset = tileData.HorizontalFlip
                         ? 7 - i
                         : i;

        var x = (tilemapColumn * TILE_SIZE) + columnOffset;
        if (allowHScroll)
          x += HScroll;
        x %= HORIZONTAL_RESOLUTION;

        if (BlankLeftColumn && x < TILE_SIZE)
        {
          SetPixel(x, BlankColor, false);
          continue;
        }

        var paletteIndex = patternData.GetPaletteIndex(7 - i);

        if (_spriteMask[x] &&
            (!tileData.HighPriority || paletteIndex == TRANSPARENT))
          continue;

        if (tileData.UseSpritePalette)
          paletteIndex += 16;

        SetPixel(x, paletteIndex, false);
      }
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeLegacySprites()
  {
    var spriteHeight = TILE_SIZE;
    if (StretchSprites)
      spriteHeight *= 2;

    var spriteCount = 0;
    for (int sprite = 0; sprite < 32; sprite++)
    {
      var baseAddress = SpriteAttributeTableAddress + (sprite * 4);
      ushort y = _vram[baseAddress];
      if (y == DISABLE_SPRITES)
      {
        if (!SpriteOverflow)
          SetLastSpriteIndex(sprite);
        return;
      }

      y++;
      if (y >= DISABLE_SPRITES)
        y -= 0x100;

      if (y > _vCounter ||
          y + spriteHeight <= _vCounter)
        continue;

      var x = _vram[baseAddress + 1];
      var pattern = _vram[baseAddress + 2];
      var color = _vram[baseAddress + 3];

      if (color.TestBit(7))
        x -= 32;

      color &= 0b_0000_1111;
      if (color == TRANSPARENT)
        continue;

      spriteCount++;
      if (spriteCount > 4)
      {
        SetLastSpriteIndex(sprite);
        SpriteOverflow = true;
        return;
      }
      else
        SpriteOverflow = false;

      var offset = _vCounter - y;
      if (spriteHeight == TILE_SIZE)
      {
        var address = LegacySpritePatternTableAddress + (pattern * TILE_SIZE);
        RasterizeMode2Sprite(address, x, offset, color);
      }
      else
      {
        var address = LegacySpritePatternTableAddress + ((pattern & 0b_1111_1100) * TILE_SIZE);
        RasterizeMode2Sprite(address, x, offset, color);
        RasterizeMode2Sprite(address, x + TILE_SIZE, offset + 16, color);
      }
    }
    
    if (!SpriteOverflow)
      SetLastSpriteIndex(31);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeMode2Sprite(int address, int x, int offset, byte color)
  {
    var data = _vram[address + offset];
    for (byte i = 0; i < TILE_SIZE; i++)
    {
      if (_spriteMask[x + i])
      {
        SpriteCollision = true;
        continue;
      }

      if (!data.TestBit(7 - i))
        continue;

      SetLegacyPixel(x + i, color, true);
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeLegacyBackground()
  {
    var colorMask = (_registers[0x3] << 1) | 1;

    var row = _vCounter / TILE_SIZE;
    var rowOffset = _vCounter % TILE_SIZE;

    var tableAddressOffset = row switch
    {
      < 8   => 0,
      < 16  => TestRegisterBit(0x4, 1) ? 0x800 : 0,
      _     => TestRegisterBit(0x4, 0) ? 0x1000 : 0
    };

    for (int column = 0; column < BACKGROUND_COLUMNS; column++)
    {
      var patternIndex = _vram[NameTableAddress + column + (row * BACKGROUND_COLUMNS)];
      var patternAddress = PatternTableAddress + tableAddressOffset;
      patternAddress += rowOffset + (patternIndex * TILE_SIZE);

      var colorIndex = patternIndex & colorMask;
      var colorAddress = ColorTableAddress + tableAddressOffset;
      colorAddress += rowOffset + (colorIndex * TILE_SIZE);
      
      var patternData = _vram[patternAddress];
      var colorData = _vram[colorAddress];

      var x = column * TILE_SIZE;
      var tileEnd = x + TILE_SIZE;
      for (byte i = TILE_SIZE - 1; x < tileEnd; x++, i--)
      {
        if (x >= HORIZONTAL_RESOLUTION)
          return;

        var color = patternData.TestBit(i)
                  ? colorData.HighNibble()
                  : colorData.LowNibble();
        
        if (_spriteMask[x])
          continue;
        
        if (color == TRANSPARENT)
          color = LegacyBlankColor;

        SetLegacyPixel(x, color, false);
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
  private void SetPixel(int x, int paletteIndex, bool isSprite)
  {
    _scanline[x] = _palette[paletteIndex];
    _spriteMask[x] = isSprite;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SetLegacyPixel(int x, byte color, bool isSprite)
  {
    _scanline[x] = _palette[Color.ToLegacyRGBA(color)];
    _spriteMask[x] = isSprite;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void CommitScanline()
  {
    _framebuffer.BlitScanline(_vCounter, _scanline);
    Array.Clear(_spriteMask);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SetLastSpriteIndex(int value)
  {
    _status &= Status.All;
    _status |= (Status)value;
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
  private void IncrementAddress() => _addressBus = (ushort)((_addressBus + 1) % VRAM_SIZE);

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