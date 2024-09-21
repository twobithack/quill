using Quill.Common.Extensions;
using Quill.Video.Definitions;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Quill.Video;

public sealed partial class VDP
{
  public VDP(int extraScanlines)
  {
    _framebuffer = new Framebuffer(HORIZONTAL_RESOLUTION, _vCounterActive);
    _palette = new int[CRAM_SIZE];
    _vram = new byte[VRAM_SIZE];
    _registers = new byte[REGISTER_COUNT];
    _vCounterMax += (_vCounterJumpStart - _vCounterJumpEnd);
    _vCounterMax += extraScanlines;
  }

  #region Methods
  public byte ReadStatus()
  {
    _controlWritePending = false;

    var value = (byte)_status;
    _status &= ~Status.All;
    IRQ = false;
    return value;
  }

  public void WriteControl(byte value)
  {
    if (!_controlWritePending)
    {
      _controlWord &= 0b_1111_1111_0000_0000;
      _controlWord |= value;
      _controlWritePending = true;
      return;
    }
    
    _controlWord &= 0b_0000_0000_1111_1111;
    _controlWord |= (ushort)(value << 8);
    _controlWritePending = false;
    
    if (ControlCode == ControlCode.ReadVRAM)
    {
      _dataBuffer = _vram[Address];
      IncrementAddress();
    }
    else if (ControlCode == ControlCode.WriteRegister)
    {
      var register = _controlWord.HighByte().LowNibble();
      if (register >= REGISTER_COUNT)
        return;

      _registers[register] = _controlWord.LowByte();

      if (register == 0x1)
        IRQ = VSyncPending;
    }
  }

  public byte ReadData()
  {
    _controlWritePending = false;

    var data = _dataBuffer;
    _dataBuffer = _vram[Address];
    IncrementAddress();
    return data;
  }

  public void WriteData(byte value)
  {
    _controlWritePending = false;

    if (ControlCode == ControlCode.WriteCRAM)
    {
      var index = _controlWord & 0b_0001_1111;
      _palette[index] = Color.ToRGBA(value);
    }
    else
      _vram[Address] = value;

    _dataBuffer = value;
    IncrementAddress();
  }

  public void RenderScanline()
  {
    IRQ = false;
    _vCounter++;

    if (_vCounter == _vCounterActive)
    {
      _framebuffer.PushFrame();
    }
    else if (_vCounter == _vCounterActive + 1)
    {
      VBlank = true;
    }
    else if (_vCounter == _vCounterJumpStart)
    {
      if (!_vCounterJumped)
      {
        _vCounter = _vCounterJumpEnd;
        _vCounterJumped = true;
      }
    }
    else if (_vCounter == _vCounterMax)
    {
      _vCounter = 0;
      _vCounterJumped = false;
      _vScroll = _registers[0x9];
    }

    if (_vCounter > _vCounterActive)
    {
      _lineInterrupt = _registers[0xA];
    }
    else if (_lineInterrupt == 0)
    {
      _lineInterrupt = _registers[0xA];
      IRQ = LineInterruptEnabled;
    }
    else
    {
      _lineInterrupt--;
    }
     
    if (_vCounter < _vCounterActive)
    {
      _hScroll = _registers[0x8];
      if (DisplayEnabled)
        RasterizeScanline();
    }

    IRQ |= VSyncPending;
  }

  public byte[] ReadFramebuffer() => _framebuffer.PopFrame();

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeScanline()
  {
    var mode = GetDisplayMode();
    if (mode == DisplayMode.Graphic_2)
    {
      RasterizeSpritesMode2();
      RasterizeBackgroundMode2();
    }
    else
    {
      RasterizeSpritesMode4();
      RasterizeBackgroundMode4();
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeSpritesMode2()
  {
    var spriteHeight = StretchSprites ? 16 : TILE_SIZE;
    var satAddress = GetSpriteAttributeTableAddress();
    var sgtAddress = GetSpriteGeneratorTableAddress();

    var spriteCount = 0;
    for (int sprite = 0; sprite < 32; sprite++)
    {
      var baseAddress = satAddress + (sprite * 4);
      ushort y = _vram[baseAddress];
      if (y == DISABLE_SPRITES)
      {
        if (!SpriteOverflow)
          StoreLastSpriteIndex(sprite);
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
        StoreLastSpriteIndex(sprite);
        SpriteOverflow = true;
        return;
      }
      else
        SpriteOverflow = false;

      var offset = _vCounter - y;
      if (spriteHeight == TILE_SIZE)
      {
        var spriteAddress = sgtAddress + (pattern * 8);
        var data = _vram[spriteAddress + offset];

        for (byte i = 0; i < TILE_SIZE; i++)
        {
          if (_framebuffer.CheckCollision(x + i, _vCounter))
          {
            SpriteCollision = true;
            continue;
          }
          
          if (!data.TestBit(7 - i))
            continue;

          _framebuffer.SetLegacyPixel(x + i, _vCounter, color, true);
        }
      }
    }
    
    StoreLastSpriteIndex(31);
  }

  private void RasterizeSpriteMode2()
  {

  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeSpritesMode4()
  {
    var spriteHeight = (StretchSprites || ZoomSprites) ? 16 : TILE_SIZE;
    var satAddress = GetSpriteAttributeTableAddress();

    var spriteCount = 0;
    for (int sprite = 0; sprite < 64; sprite++)
    {
      ushort y = _vram[satAddress + sprite];
      if (y == DISABLE_SPRITES)
      {
        // var mode = GetDisplayMode();
        // if (mode != DisplayMode.Mode_4_224 &&
        //     mode != DisplayMode.Mode_4_240)
          return;
      }

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
      int x = _vram[satAddress + offset];
      int patternIndex = _vram[satAddress + offset + 1];

      if (ShiftX)
        x -= TILE_SIZE;

      if (UseSecondPatternTable)
        patternIndex += 0x100;

      if (StretchSprites && y <= _vCounter + TILE_SIZE)
        patternIndex &= 0b_1111_1111_1111_1110;

      var patternAddress = patternIndex * 32;
      patternAddress += (_vCounter - y) * 4;
      var patternData = GetPatternData(patternAddress);

      var spriteEnd = x + TILE_SIZE;
      for (byte column = TILE_SIZE - 1; x < spriteEnd; x++, column--)
      {
        if (x >= HORIZONTAL_RESOLUTION)
          break;

        if (x < 8 && MaskLeftBorder)
          continue;

        var paletteIndex = patternData.GetPaletteIndex(column);
        if (paletteIndex == TRANSPARENT)
          continue;
        paletteIndex += 16;

        if (_framebuffer.CheckCollision(x, _vCounter))
        {
          SpriteCollision = true;
          continue;
        }

        SetPixel(x, _vCounter, paletteIndex, true);
      }
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeBackgroundMode2()
  {
    // TODO
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeBackgroundMode4()
  {
    var baseAddress = GetNameTableAddress();
    var allowHScroll = !LimitHScroll ||
                       _vCounter / TILE_SIZE > HSCROLL_LIMIT;

    for (int column = 0; column < BACKGROUND_COLUMNS; column++)
    {
      int tilemapY = _vCounter;
      if (!LimitVScroll ||
          column < VSCROLL_LIMIT)
        tilemapY += _vScroll;

      var tilemapRow = tilemapY / TILE_SIZE;
      if (tilemapRow >= _backgroundRows) 
        tilemapRow -= _backgroundRows;
                    
      var tilemapColumn = column;
      if (allowHScroll)
        tilemapColumn -= _hScroll / TILE_SIZE;

      tilemapColumn %= BACKGROUND_COLUMNS;
      if (tilemapColumn < 0) 
        tilemapColumn += BACKGROUND_COLUMNS;

      var tileAddress = baseAddress + 
                        (tilemapRow * BACKGROUND_COLUMNS * 2) + 
                        (tilemapColumn * 2);

      var tile = GetTileData(tileAddress);
      var tileRow = tile.VerticalFlip
                  ? 7 - (tilemapY % TILE_SIZE)
                  : tilemapY % TILE_SIZE;

      var patternAddress = (tile.PatternIndex * 32) + (tileRow * 4);
      var patternData = GetPatternData(patternAddress);

      for (int i = 0; i < TILE_SIZE; i ++)
      {
        var columnOffset = i;
        if (tile.HorizontalFlip)
          columnOffset = 7 - columnOffset;

        var x = (tilemapColumn * TILE_SIZE) + columnOffset;
        if (allowHScroll)
          x += _hScroll;
        x %= HORIZONTAL_RESOLUTION;

        if (x < 0)
          throw new Exception();

        if (x < TILE_SIZE && MaskLeftBorder)
          continue;

        if (!tile.HighPriotity && 
            _framebuffer.CheckCollision(x, _vCounter))
          continue;

        var paletteIndex = patternData.GetPaletteIndex(7 - i);
        if (paletteIndex == TRANSPARENT)
        {
          if (_framebuffer.CheckCollision(x, _vCounter))
            continue;
          paletteIndex = BackgroundColor;
        }
        
        if (tile.UseSpritePalette)
          paletteIndex += 16;

        SetPixel(x, _vCounter, paletteIndex, false);
      }
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SetPixel(int x, int y, int paletteIndex, bool sprite) => _framebuffer.SetPixel(x, y, _palette[paletteIndex], sprite);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SetLegacyPixel(int x, int y, byte color, bool sprite) => _framebuffer.SetLegacyPixel(x, y, color, sprite);

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
  private DisplayMode GetDisplayMode()
  {
    byte mode = 0x00;
    if (TestRegisterBit(0x0, 2))
      mode |= 0b_1000;
    if (TestRegisterBit(0x1, 3))
      mode |= 0b_0100;
    if (TestRegisterBit(0x0, 1))
      mode |= 0b_0010;
    if (TestRegisterBit(0x1, 4))
      mode |= 0b_0001;

    return (DisplayMode)mode;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private ushort GetNameTableAddress()
  {
    var addressMask = 0b_0000_1110;

    // var mode = GetDisplayMode();
    // if (mode == 0b_1011 ||
    //     mode == 0b_1110)
    //   addressMask = 0b_0000_1100;

    var address = _registers[0x2] & addressMask;
    return (ushort)(address << 10);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private ushort GetColorTableAddress() => (ushort)(_registers[0x3] << 6);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private ushort GetPatternGeneratorTableAddress()
  {
    var address = _registers[0x4] & 0b_0000_0111;
    return (ushort)(address << 11);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private ushort GetSpriteAttributeTableAddress()
  {
    var address = _registers[0x5] & 0b_0111_1110;
    return (ushort)(address << 7);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private ushort GetSpriteGeneratorTableAddress()
  {
    var address = _registers[0x6] & 0b_0000_0111;
    return (ushort)(address << 11);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void IncrementAddress()
  {
    if (Address + 1 == VRAM_SIZE)
      _controlWord &= 0b_1100_0000_0000_0000;
    else
      _controlWord++;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private bool TestRegisterBit(byte register, byte bit) => _registers[register].TestBit(bit);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SetFlag(Status flag, bool value) => _status = value
                                                 ? _status | flag
                                                 : _status & ~flag;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void StoreLastSpriteIndex(int value)
  {
    _status &= Status.All;
    _status |= (Status)value;
  }
  #endregion
}