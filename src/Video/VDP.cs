using System.Linq;
using System.Runtime.CompilerServices;

using Quill.Common.Extensions;
using Quill.Video.Definitions;

namespace Quill.Video;

public sealed partial class VDP
{
  public VDP()
  {
    _framebuffer = new Framebuffer();
    _palette = new int[CRAM_SIZE];
    _vram = new byte[VRAM_SIZE];
    _registers = new byte[REGISTER_COUNT];
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

  public void Step(int clockCycles)
  {
    _hCounter += (ushort)(clockCycles * 3);

    if (_hCounter < HCOUNTER_MAX)
      return;
      
    _hCounter -= HCOUNTER_MAX;
    RenderScanline();
  }

  public void AcknowledgeFrame() => FrameCompleted = false;

  public byte[] ReadFramebuffer() => _framebuffer.PopFrame();

  private void RenderScanline()
  {
    IncrementScanline();
    UpdateInterrupts();

    if (_vCounter <= _vCounterActive)
      RasterizeScanline();
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void IncrementScanline()
  {
    if (_vCounter == _vCounterActive)
    {
      _framebuffer.PushFrame();
    }
    if (_vCounter == _vCounterActive + 1)
    {
      VBlank = true;
    }
    else if (_vCounter == _vCounterJumpFrom)
    {
      if (!_vCounterJumped)
      {
        _vCounter = _vCounterJumpTo;
        _vCounterJumped = true;
        return;
      }
    }
    else if (_vCounter == VCOUNTER_MAX)
    {
      _vCounterJumped = false;
      _vScroll = _registers[0x9];
      FrameCompleted = true;
    }
    
    _vCounter++;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void UpdateInterrupts()
  {
    if (_vCounter > _vCounterActive + 1 && 
        _vCounter <= VCOUNTER_MAX)
    {
      _hLineCounter = _registers[0xA];
    }
    else if (_hLineCounter == 0)
    {
      _hLineCounter = _registers[0xA];
      _hLineInterruptPending = _hLineInterruptEnabled;
    }
    else
      _hLineCounter--;

    IRQ = VSyncPending || HLinePending;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeScanline()
  { 
    if (!_displayEnabled)
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
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void BlankScanline()
  {
    if (DisplayMode4)
    {
      for (int x = 0; x < HORIZONTAL_RESOLUTION; x++)
        SetPixel(x, _vCounter, _blankColor, false);
    }
    else
    {
      for (int x = 0; x < HORIZONTAL_RESOLUTION; x++)
        SetLegacyPixel(x, _vCounter, _legacyBlankColor, false);
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeSprites()
  {
    var spriteHeight = TILE_SIZE;
    if (_stretchSprites || _zoomSprites)
      spriteHeight *= 2;

    var spriteCount = 0;
    for (int sprite = 0; sprite < 64; sprite++)
    {
      ushort y = _vram[_spriteAttributeTableAddress + sprite];
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
      {
        SpriteOverflow = true;
        return;
      }

      var offset = 0x80 + (sprite * 2);
      int x = _vram[_spriteAttributeTableAddress + offset];
      int patternIndex = _vram[_spriteAttributeTableAddress + offset + 1];

      if (_spriteShift)
        x -= TILE_SIZE;

      if (_useSecondPatternTable)
        patternIndex += 0x100;

      if (_stretchSprites && y <= _vCounter + TILE_SIZE)
        patternIndex &= 0b_1111_1111_1111_1110;

      var patternAddress = patternIndex * 32;
      patternAddress += (_vCounter - y) * 4;
      var patternData = GetPatternData(patternAddress);

      var spriteEnd = x + TILE_SIZE;
      for (byte i = TILE_SIZE - 1; x < spriteEnd; x++, i--)
      {
        if (x >= HORIZONTAL_RESOLUTION)
          break;

        if (_leftColumnBlank && x < TILE_SIZE)
          continue;

        var paletteIndex = patternData.GetPaletteIndex(i);
        if (paletteIndex == TRANSPARENT)
          continue;
        paletteIndex += 16;

        if (_framebuffer.IsOccupied(x, _vCounter))
        {
          SpriteCollision = true;
          continue;
        }

        SetPixel(x, _vCounter, paletteIndex, true);
      }
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeBackground()
  {
    var allowHScroll = !_hScrollInhibit ||
                       _vCounter / TILE_SIZE > HSCROLL_LIMIT;

    for (int backgroundColumn = 0; backgroundColumn < BACKGROUND_COLUMNS; backgroundColumn++)
    {
      ushort tilemapY = _vCounter;
      if (!_vScrollInhibit ||
          backgroundColumn < VSCROLL_LIMIT)
        tilemapY += _vScroll;

      var tilemapRow = tilemapY / TILE_SIZE;
      if (tilemapRow >= _backgroundRows) 
        tilemapRow -= _backgroundRows;
                    
      var tilemapColumn = backgroundColumn;
      if (allowHScroll)
        tilemapColumn += BACKGROUND_COLUMNS - (_hScroll / TILE_SIZE);
      tilemapColumn %= BACKGROUND_COLUMNS;

      var tileAddress = _nameTableAddress + 
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
          x += _hScroll;
        x %= HORIZONTAL_RESOLUTION;

        if (_leftColumnBlank && x < TILE_SIZE)
        {
          SetPixel(x, _vCounter, _blankColor, false);
          continue;
        }

        var paletteIndex = patternData.GetPaletteIndex(7 - i);

        if (_framebuffer.IsOccupied(x, _vCounter) &&
            (!tileData.HighPriotity || paletteIndex == TRANSPARENT))
          continue;

        if (tileData.UseSpritePalette)
          paletteIndex += 16;

        SetPixel(x, _vCounter, paletteIndex, false);
      }
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeLegacySprites()
  {
    var spriteHeight = TILE_SIZE;
    if (_stretchSprites)
      spriteHeight *= 2;

    var spriteCount = 0;
    for (int sprite = 0; sprite < 32; sprite++)
    {
      var baseAddress = _spriteAttributeTableAddress + (sprite * 4);
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
        var address = _spriteGeneratorTableAddress + (pattern * TILE_SIZE);
        RasterizeMode2Sprite(address, x, offset, color);
      }
      else
      {
        var address = _spriteGeneratorTableAddress + ((pattern & 0b_1111_1100) * TILE_SIZE);
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
      if (_framebuffer.IsOccupied(x + i, _vCounter))
      {
        SpriteCollision = true;
        continue;
      }

      if (!data.TestBit(7 - i))
        continue;

      SetLegacyPixel(x + i, _vCounter, color, true);
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
      var patternIndex = _vram[_nameTableAddress + column + (row * BACKGROUND_COLUMNS)];
      var patternAddress = _patternGeneratorTableAddress + tableAddressOffset;
      patternAddress += rowOffset + (patternIndex * TILE_SIZE);

      var colorIndex = patternIndex & colorMask;
      var colorAddress = _colorTableAddress + tableAddressOffset;
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
        
        if (_framebuffer.IsOccupied(x, _vCounter))
          continue;
        
        if (color == TRANSPARENT)
          color = _legacyBlankColor;

        SetLegacyPixel(x, _vCounter, color, false);
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
  private void SetPixel(int x, int y, int paletteIndex, bool isSprite) => _framebuffer.SetPixel(x, y, _palette[paletteIndex], isSprite);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SetLegacyPixel(int x, int y, byte color, bool isSprite) => _framebuffer.SetPixel(x, y, Color.ToLegacyRGBA(color), isSprite);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private bool GetFlag(Status flag) => (_status & flag) != 0;
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SetFlag(Status flag, bool value) => _status = value
                                                 ? _status | flag
                                                 : _status & ~flag;

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
    switch (register)
    {
      case 0x0:
        _spriteShift = value.TestBit(3);
        _hLineInterruptEnabled = value.TestBit(4);
        _leftColumnBlank = value.TestBit(5);
        _hScrollInhibit = value.TestBit(6);
        _vScrollInhibit = value.TestBit(7);
        if (IRQ && !_hLineInterruptEnabled)
          IRQ = VSyncPending;
        UpdateDisplayMode();
        return;

      case 0x1:
        _zoomSprites = value.TestBit(0);
        _stretchSprites = value.TestBit(1);
        _vBlankInterruptEnabled = value.TestBit(5);
        _displayEnabled = value.TestBit(6);
        if (IRQ && !_vBlankInterruptEnabled)
          IRQ = HLinePending;
        UpdateDisplayMode();
        return;

      case 0x2:
        _nameTableAddress = (ushort)(value & 0b_0000_1110);
        _nameTableAddress <<= 10;
        return;

      case 0x3:
        _colorTableAddress = value.TestBit(7)
                           ? (ushort)0x2000
                           : (ushort)0x0000;
        return;

      case 0x4:
        _patternGeneratorTableAddress = value.TestBit(2)
                                      ? (ushort)0x2000
                                      : (ushort)0x0000;
        return;

      case 0x5:
        _spriteAttributeTableAddress = (ushort)(value & 0b_0111_1110);
        _spriteAttributeTableAddress <<= 7;
        return;

      case 0x6:
        _useSecondPatternTable = value.TestBit(2);
        _spriteGeneratorTableAddress = (ushort)(value & 0b_0000_0111);
        _spriteGeneratorTableAddress <<= 11;
        return;

      case 0x7:
        _legacyBlankColor = (byte)(value & 0b_1111);
        _blankColor = _legacyBlankColor.SetBit(4);
        return;

      case 0x8:
        _hScroll = value;
        return;
    };
  }
  #endregion
}