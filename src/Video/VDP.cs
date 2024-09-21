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
    _vram = new byte[VRAM_SIZE];
    _cram = new Color[CRAM_SIZE];
    _registers = new byte[REGISTER_COUNT];
    _framebuffer = new byte[FRAMEBUFFER_SIZE];
    _renderbuffer = new byte[FRAMEBUFFER_SIZE];
    _vCounterMax += (_vCounterJumpStart - _vCounterJumpEnd);
    _vCounterMax += extraScanlines;
  }

  #region Methods
  public byte ReadStatus()
  {
    var status = (byte)_status;
    _status = Status.None;
    _controlWritePending = false;
    IRQ = false;
    return status;
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
        IRQ = VSyncEnabled && VSyncPending;
    }
  }

  public byte ReadData()
  {
    var data = _dataBuffer;
    _controlWritePending = false;
    _dataBuffer = _vram[Address];
    IncrementAddress();
    return data;
  }

  public void WriteData(byte value)
  {
    _controlWritePending = false;
    _dataBuffer = value;

    if (ControlCode == ControlCode.WriteCRAM)
    {
      var index = _controlWord & 0b_0001_1111;
      _cram[index].Set(value);
    }
    else
      _vram[Address] = value;

    IncrementAddress();
  }

  public void RenderScanline()
  {
    IRQ = false;
    _vCounter++;

    if (_vCounter == _vCounterMax)
    {
      _vCounter = 0;
      _vCounterJumped = false;
    }
    else if (!_vCounterJumped && _vCounter == _vCounterJumpStart)
    {
      _vCounter = _vCounterJumpEnd;
      _vCounterJumped = true;
    }
    else if (_vCounter == _vCounterActive)
    {
      RenderFrame();
    }
    else if (_vCounter == _vCounterActive + 1)
    {
      VSyncPending = true;
    }

    if (_vCounter > _vCounterActive)
    {
      _lineInterrupt = _registers[0xA];
    }
    else if (_lineInterrupt == 0x00)
    {
      _lineInterrupt = _registers[0xA];
      if (LineInterruptEnabled)
        IRQ = true;
    }
    else
    {
      _lineInterrupt--;
    }
     
    if (_vCounter >= _vCounterActive)
    {
      _vScroll = _registers[0x9];
    }
    else if (DisplayEnabled)
    {
      _hScroll = _registers[0x8];
      RasterizeScanline();
    }

    if (!IRQ &&
        VSyncEnabled && 
        VSyncPending)
      IRQ = true;
  }

  public byte[] ReadFramebuffer()
  {
    lock (_framebuffer)
    {
      if (!_frameQueued)
        return null;

      _frameQueued = false;
      return _framebuffer;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RenderFrame()
  {
    lock (_framebuffer)
    {
      Buffer.BlockCopy(_renderbuffer, 0, _framebuffer, 0, FRAMEBUFFER_SIZE);
      _frameQueued = true;
    }
    
    Array.Clear(_renderbuffer);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeScanline()
  {
    // TODO: Mode 1+2 support
    RasterizeSprites();
    RasterizeBackground();
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeSprites()
  {
    var spriteHeight = (StretchSprites || ZoomSprites) ? 16 : 8;
    var spriteCount = 0;

    var baseAddress = GetSpriteAttributeTableAddress();
    for (int sprite = 0; sprite < 64; sprite++)
    {
      ushort y = _vram[baseAddress + sprite];
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
        _status |= Status.Overflow;

      var offset = 0x80 + (sprite * 2);
      int x = _vram[baseAddress + offset];
      int patternIndex = _vram[baseAddress + offset + 1];

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
      for (byte i = TILE_SIZE - 1; x < spriteEnd; x++, i--)
      {
        if (x >= HORIZONTAL_RESOLUTION)
          break;

        if (x < 8 && MaskLeftBorder)
          continue;

        var paletteIndex = patternData.GetPaletteIndex(i);
        if (paletteIndex == TRANSPARENT)
          continue;

        var pixelIndex = GetPixelIndex(x, _vCounter);
        if (_renderbuffer[pixelIndex + 3] == OCCUPIED)
        {
          _status |= Status.Collision;
          continue;
        }

        SetPixelColor(pixelIndex, paletteIndex + 16, true);
      }
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeBackground()
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

        var pixelIndex = GetPixelIndex(x, _vCounter);
        if (!tile.HighPriotity && 
            _renderbuffer[pixelIndex + 3] == OCCUPIED)
          continue;

        var paletteIndex = patternData.GetPaletteIndex(7 - i);
        if (paletteIndex == TRANSPARENT)
        {
          if (_renderbuffer[pixelIndex + 3] != OCCUPIED)
            paletteIndex = BackgroundColor;
          else
            continue;
        }
        
        if (tile.UseSpritePalette)
          paletteIndex += 16;

        SetPixelColor(pixelIndex, paletteIndex, false);
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
  private void SetPixelColor(int pixelIndex, int paletteIndex, bool sprite)
  {
    var color = _cram[paletteIndex];
    _renderbuffer[pixelIndex] = color.Red;
    _renderbuffer[pixelIndex + 1] = color.Green;
    _renderbuffer[pixelIndex + 2] = color.Blue;

    if (sprite)
      _renderbuffer[pixelIndex + 3] = OCCUPIED;
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
  private bool TestRegisterBit(byte register, byte bit) => _registers[register].TestBit(bit);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static int GetPixelIndex(int x, int y) => (x + (y * 256)) * 4;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private ushort GetSpriteAttributeTableAddress()
  {
    var address = _registers[0x5] & 0b_0111_1110;
    return (ushort)(address << 7);
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
  #endregion
}