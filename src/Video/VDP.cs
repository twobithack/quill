using Quill.Common;
using Quill.Core;
using Quill.Video.Definitions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Quill.Video;

unsafe public class VDP
{
  #region Constants
  private const int FRAMEBUFFER_SIZE = 0x30000;
  private const int VRAM_SIZE = 0x4000;
  private const int CRAM_SIZE = 0x20;
  private const int REGISTER_COUNT = 11;
  private const int HORIZONTAL_RESOLUTION = 256;
  private const int TILE_SIZE = 8;
  private const int BACKGROUND_COLUMNS = 32;
  private const int HSCROLL_LIMIT = 1;
  private const int VSCROLL_LIMIT = 24;
  private const int DISABLE_SPRITES = 0xD0;
  #endregion

  #region Fields
  public bool IRQ;

  private readonly byte[] _vram;
  private readonly Color[] _cram;
  private readonly byte[] _registers;
  private readonly byte[] _framebuffer;
  private readonly byte[] _renderbuffer;
  private readonly ushort _hCounter;
  private ushort _vCounter;
  private ushort _controlWord;
  private Status _status;
  private byte _dataBuffer;
  private byte _lineInterrupt;
  private byte _hScroll;
  private byte _vScroll;
  private bool _controlWritePending;
  private bool _vCounterJumped;
  private bool _frameQueued;

  // TODO: derive from display mode
  private readonly int _backgroundRows = 28;
  private readonly int _vCounterMax = 255;
  private readonly byte _vCounterActive = 192;
  private readonly byte _vCounterJumpStart = 0xDA;
  private readonly byte _vCounterJumpEnd = 0xD5;
  #endregion

  #region Constructors
  public VDP(int extraScanlines)
  {
    _vram = new byte[VRAM_SIZE];
    _cram = new Color[CRAM_SIZE];
    _registers = new byte[REGISTER_COUNT];
    _framebuffer = new byte[FRAMEBUFFER_SIZE];
    _renderbuffer = new byte[FRAMEBUFFER_SIZE];
    _vCounterMax += (_vCounterJumpStart - _vCounterJumpEnd);
    _vCounterMax += extraScanlines;
    _hCounter = 0x00;
  }
  #endregion

  #region Properties
  public byte HCounter => (byte)(_hCounter >> 1);
  public byte VCounter => _vCounter > byte.MaxValue ? byte.MaxValue : (byte)_vCounter;
  public int ScanlinesPerFrame => _vCounterMax + (_vCounterJumpStart - _vCounterJumpEnd);

  private ControlCode ControlCode => (ControlCode)(_controlWord >> 14);
  private ushort Address => (ushort)(_controlWord & 0b_0011_1111_1111_1111);
  private bool ShiftX => TestRegisterBit(0x0, 3);
  private bool LineInterruptEnabled => TestRegisterBit(0x0, 4);
  private bool MaskLeftBorder => TestRegisterBit(0x0, 5);
  private bool LimitHScroll => TestRegisterBit(0x0, 6);
  private bool LimitVScroll => TestRegisterBit(0x0, 7);
  private bool ZoomSprites => TestRegisterBit(0x1, 0);
  private bool StretchSprites => TestRegisterBit(0x1, 1);
  private bool VSyncEnabled => TestRegisterBit(0x1, 5);
  private bool DisplayEnabled => TestRegisterBit(0x1, 6);
  private bool UseSecondPatternTable => TestRegisterBit(0x6, 2);
  private byte BackgroundColor => (byte)(_registers[0x7] & 0b_0011);

  private bool VSyncPending
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => _status.HasFlag(Status.VSync);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set
    {
      if (value)
        _status |= Status.VSync;
      else
        _status &= ~Status.VSync;
    } 
  }
  #endregion

  #region Methods
  public void LoadState(Snapshot snapshot)
  {
    Array.Copy(snapshot.CRAM, _cram, _cram.Length);
    Array.Copy(snapshot.VRAM, _vram, _vram.Length);
    Array.Copy(snapshot.VDPRegisters, _registers, _registers.Length);
    _status = snapshot.VDPStatus;
    _dataBuffer = snapshot.DataPort;
    _lineInterrupt = snapshot.LineInterrupt;
    _hScroll = snapshot.HScroll;
    _vScroll = snapshot.VScroll;
    _controlWritePending = snapshot.ControlWritePending;
  }

  public void SaveState(ref Snapshot snapshot)
  {
    snapshot.CRAM = _cram;
    snapshot.VRAM = _vram;
    snapshot.VDPRegisters = _registers;
    snapshot.VDPStatus = _status;
    snapshot.DataPort = _dataBuffer;
    snapshot.LineInterrupt = _lineInterrupt;
    snapshot.HScroll = _hScroll;
    snapshot.VScroll = _vScroll;
    snapshot.ControlWritePending = _controlWritePending;
  }

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

    if (ControlCode == ControlCode.WriteVRAM)
      _dataBuffer = _vram[Address];
    else if (ControlCode == ControlCode.WriteRegister)
    {
      var register = _controlWord.HighByte().LowNibble();
      if (register >= REGISTER_COUNT)
        return;

      _registers[register] = _controlWord.LowByte();

      if (register == 0x0 &&
          VSyncEnabled && 
          VSyncPending)
        IRQ = true;
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
        var mode = GetDisplayMode();
        if (mode != DisplayMode.Mode_4_224 &&
            mode != DisplayMode.Mode_4_240)
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

        var pixelIndex = GetPixelIndex(x, _vCounter);
        if (_renderbuffer[pixelIndex + 3] != 0x00)
        {
          _status |= Status.Collision;
          continue;
        }

        var paletteIndex = patternData.GetPaletteIndex(i);
        if (paletteIndex == 0x00)
          continue;

        SetPixelColor(pixelIndex, paletteIndex + 16, 0xFF);
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

      var rowOffset = tilemapY % TILE_SIZE;
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
                  ? 7 - rowOffset
                  : rowOffset;

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
        if (!tile.HighPriotity && _renderbuffer[pixelIndex + 3] != 0x00)
          continue;

        var paletteIndex = patternData.GetPaletteIndex(7 - i);
        if (paletteIndex == 0x00)
        {
          if (_renderbuffer[pixelIndex + 3] == 0x00)
            paletteIndex = BackgroundColor;
          else
            continue;
        }
        
        if (tile.UseSpritePalette)
          paletteIndex += 16;

        SetPixelColor(pixelIndex, paletteIndex);
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
  private void SetPixelColor(int pixelIndex, int paletteIndex, byte? alpha = null)
  {
    var color = _cram[paletteIndex];
    _renderbuffer[pixelIndex] = color.Red;
    _renderbuffer[pixelIndex + 1] = color.Green;
    _renderbuffer[pixelIndex + 2] = color.Blue;

    if (alpha != null)
      _renderbuffer[pixelIndex + 3] = alpha.Value;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void GenerateNoise()
  {
    var random = new Random();
    for (int index = 0; index < _renderbuffer.Length; index += 4)
      _renderbuffer[index] = _renderbuffer[index+1] = _renderbuffer[index+2] = (byte)(byte.MaxValue * random.NextSingle());
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

    // Doesn't yet seem to do anything?
    // var mode = GetDisplayMode();
    // if (mode == 0b_1011 ||
    //     mode == 0b_1110)
    //   addressMask = 0b_0000_1100;

    var address = _registers[0x2] & addressMask;
    return (ushort)(address << 10);
  }

  private void DumpVRAM(string path)
  {
    var memory = new List<string>();
    var row = string.Empty;
    
    for (ushort address = 0; address < VRAM_SIZE; address++)
    {
      if (address % 16 == 0)
      {
        memory.Add(row);
        row = $"{address.ToHex()} | ";
      }
      row += _vram[address].ToHex();
    }

    File.WriteAllLines(path, memory);
  }

  public override string ToString()
  {
    var state = $"VDP | Control: {ControlCode} | Address: {Address.ToHex()} | SAT Address: {GetSpriteAttributeTableAddress().ToHex()}\r\n"; 

    for (byte register = 0; register < REGISTER_COUNT; register++)
      state += $"R{register.ToHex()}:{_registers[register].ToHex()} ";

    return state;
  }
  #endregion
}