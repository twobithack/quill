using Microsoft.Xna.Framework.Graphics;
using Quill.Common;
using Quill.Video.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using static Quill.Video.VDP;

namespace Quill.Video;

unsafe public class VDP
{
  #region Fields
  private const int FRAMEBUFFER_SIZE = 0x30000;
  private const int VRAM_SIZE = 0x4000;
  private const int CRAM_SIZE = 0x20;
  private const int REGISTER_COUNT = 11;
  private const int HORIZONTAL_RESOLUTION = 256;
  private const int BACKGROUND_COLUMNS = 32;
  private const int TILE_SIZE = 8;
  private const int HCOUNTER_MAX = 684;
  private const int SPRITES_DISABLED = 0xD0;

  public bool IRQ;
  public byte VCounter;

  private readonly byte[] _vram;
  private readonly Color[] _cram;
  private readonly byte[] _registers;
  private readonly byte[] _framebuffer;
  private readonly byte[] _renderbuffer;

  private double _cycleCount;
  private ushort _controlWord;
  private Status _status;
  private byte _dataBuffer;
  private byte _lineInterrupt;
  private ushort _hCounter;
  private byte _hScroll;
  private byte _vScroll;
  private bool _controlWritePending;
  private bool _vCounterJumped;
  private bool _frameQueued;

  // TODO: derive from display mode
  private readonly int _backgroundRows = 28;
  private readonly byte _vCounterActive = 192;
  private readonly byte _vCounterJumpStart = 0xDA;
  private readonly byte _vCounterJumpEnd = 0xD5;
  #endregion

  public VDP()
  {
    _vram = new byte[VRAM_SIZE];
    _cram = new Color[CRAM_SIZE];
    _registers = new byte[REGISTER_COUNT];
    _framebuffer = new byte[FRAMEBUFFER_SIZE];
    _renderbuffer = new byte[FRAMEBUFFER_SIZE];
  }

  #region Properties
  private ControlCode ControlCode => (ControlCode)(_controlWord >> 14);
  private ushort Address => (ushort)(_controlWord & 0b_0011_1111_1111_1111);
  public byte HCounter => (byte)(_hCounter >> 1);
  private bool ShiftX => TestRegisterBit(0x0, 3);
  private bool LineInterruptEnabled => TestRegisterBit(0x0, 4);
  private bool MaskLeftBorder => TestRegisterBit(0x0, 5);
  private bool HScrollLimit => TestRegisterBit(0x0, 6);
  private bool VScrollLimit => TestRegisterBit(0x0, 7);
  private bool ZoomSprites => TestRegisterBit(0x1, 0);
  private bool StretchSprites => TestRegisterBit(0x1, 1);
  private bool VSyncEnabled => TestRegisterBit(0x1, 5);
  private bool DisplayEnabled => TestRegisterBit(0x1, 6);
  private bool UseSecondPatternTable => TestRegisterBit(0x6, 2);
  
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

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public byte ReadStatus()
  {
    var status = (byte)_status;
    _status = Status.None;
    _controlWritePending = false;
    IRQ = false;
    return status;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public byte ReadData()
  {
    var data = _dataBuffer;
    _controlWritePending = false;
    _dataBuffer = _vram[Address];
    IncrementAddress();
    return data;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void AcknowledgeIRQ() => IRQ = false;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void Step(double cyclesElapsed)
  {
    IRQ = false;

    _cycleCount += cyclesElapsed;
    var cyclesThisUpdate = (int)_cycleCount;
    _hCounter += (ushort)(cyclesThisUpdate * 2);

    if (_hCounter > HCOUNTER_MAX)
    {
      _hCounter %= (HCOUNTER_MAX + 1);
      VCounter++;

      if (VCounter == byte.MinValue)
      {
        _vCounterJumped = false;
      }
      else if (!_vCounterJumped && VCounter == _vCounterJumpStart)
      {
        VCounter = _vCounterJumpEnd;
        _vCounterJumped = true;
      }
      else if (VCounter == _vCounterActive)
      {
        RenderFrame();
        VSyncPending = true;
      }

      if (VCounter > _vCounterActive)
        _lineInterrupt = _registers[0xA];
      else if (_lineInterrupt == 0x00)
      {
        _lineInterrupt = _registers[0xA];
        if (LineInterruptEnabled)
          IRQ = true;
      }
      else
        _lineInterrupt--;
     

      if (VCounter >= _vCounterActive)
      {
        _vScroll = _registers[0x9];
      }
      else if (DisplayEnabled)
      {
        _hScroll = _registers[0x8];
        RenderScanline();
      }
    }

    if (!IRQ &&
        VSyncEnabled && 
        VSyncPending)
      IRQ = true;

    _cycleCount -= cyclesThisUpdate;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
      Array.Copy(_renderbuffer, _framebuffer, FRAMEBUFFER_SIZE);
      #if DEBUG
      if (_frameQueued) Debug.WriteLine("Frame dropped");
      #endif
      _frameQueued = true;
    }

    if (!DisplayEnabled)
    {
      Array.Fill<byte>(_renderbuffer, 0x00);
      return;
    }

    var bgColor = _registers[0x7] & 0b_0011;
    for (var pixelIndex = 0; pixelIndex < FRAMEBUFFER_SIZE; pixelIndex += 4)
      SetPixelColor(pixelIndex, bgColor,  0x00);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RenderScanline()
  {
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
      if (y == SPRITES_DISABLED)
        return;

      y++;
      if (y >= 0xD0)
        y -= 0x100;

      if (y > VCounter ||
          y + spriteHeight <= VCounter)
        continue;

      spriteCount++;
      if (spriteCount > 8)
        _status |= Status.Overflow;

      var offset = 0x80 + (sprite * 2);
      int x = _vram[baseAddress + offset];
      int patternIndex = _vram[baseAddress + offset + 1];

      if (ShiftX)
        x -= 8;

      if (UseSecondPatternTable)
        patternIndex += 0x100;

      if (StretchSprites && y <= VCounter + TILE_SIZE)
        patternIndex &= 0b_1111_1111_1111_1110;

      var patternAddress = patternIndex * 32;
      patternAddress += (VCounter - y) * 4;
      var patternData = GetPatternData(patternAddress);

      var spriteEnd = x + 8;
      for (byte i = 7; x < spriteEnd; x++, i--)
      {
        if (x >= HORIZONTAL_RESOLUTION)
          break;

        if (x < 8 && MaskLeftBorder)
          continue;

        var pixelIndex = GetPixelIndex(x, VCounter);
        if (_renderbuffer[pixelIndex + 3] != 0x00)
        {
          _status |= Status.Collision;
          continue;
        }

        byte palette = patternData.GetPaletteIndex(i);
        if (palette == 0x00)
          continue;

        SetPixelColor(pixelIndex, palette + 16, 0xFF);
      }
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeBackground()
  {
    var baseAddress = GetNameTableAddress();
    var sourceRow = (VCounter + _vScroll) / TILE_SIZE;
    if (sourceRow >= _backgroundRows) 
      sourceRow -= _backgroundRows;
    var rowOffset = (VCounter + _vScroll) % TILE_SIZE;

    for (int destinationColumn = 0; destinationColumn < BACKGROUND_COLUMNS; destinationColumn++)
    {
      var sourceColumn = destinationColumn;
      sourceColumn -= _hScroll / TILE_SIZE;
      sourceColumn %= BACKGROUND_COLUMNS;
      if (sourceColumn < 0) 
        sourceColumn += BACKGROUND_COLUMNS;

      var tileAddress = baseAddress + 
                        (sourceRow * BACKGROUND_COLUMNS * 2) + 
                        (sourceColumn * 2);

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

        var x = (sourceColumn * TILE_SIZE) + _hScroll + columnOffset;
        x %= HORIZONTAL_RESOLUTION;

        if (x < TILE_SIZE && MaskLeftBorder)
          continue;

        var pixelIndex = GetPixelIndex(x, VCounter);
        if (!tile.HighPriotity && _renderbuffer[pixelIndex + 3] != 0x00)
          continue;

        var paletteIndex = patternData.GetPaletteIndex(7 - i);
        if (paletteIndex == 0x00)
          continue;

        if (tile.UseSpritePalette)
          paletteIndex += 16;

        SetPixelColor(pixelIndex, paletteIndex);
      }
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private Pattern GetPatternData(int patternAddress)
  {
    var row0 = _vram[patternAddress];
    var row1 = _vram[patternAddress + 1];
    var row2 = _vram[patternAddress + 2];
    var row3 = _vram[patternAddress + 3];
    return new Pattern(row0, row1, row2, row3);
  }

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
    if (Address == VRAM_SIZE - 1)
      _controlWord &= 0b_1100_0000_0000_0000;
    else
      _controlWord++;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private byte GetDisplayMode()
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
    return mode;
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
    // TODO: medium resolution support
    var address = _registers[0x2] & 0b_0000_1110;
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

  public struct Color
  {
    private const byte BITMASK = 0b_0011;
    private const byte MULTIPLIER = byte.MaxValue / 3;

    public byte Red = 0x00;
    public byte Green = 0x00;
    public byte Blue = 0x00;

    public Color() {}

    public void Set(byte color)
    {
      Red = (byte)((color & BITMASK) * MULTIPLIER);
      color >>= 2;
      Green = (byte)((color & BITMASK) * MULTIPLIER);
      color >>= 2;
      Blue = (byte)((color & BITMASK) * MULTIPLIER);
    }
  }

  public readonly struct Pattern
  {
    private readonly byte Row0 = 0x00;
    private readonly byte Row1 = 0x00;
    private readonly byte Row2 = 0x00;
    private readonly byte Row3 = 0x00;

    public Pattern(byte row0, byte row1, byte row2, byte row3)
    {
      Row0 = row0;
      Row1 = row1;
      Row2 = row2;
      Row3 = row3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetPaletteIndex(int column)
    {
      byte paletteIndex = 0x00;
      if (Row0.TestBit(column)) paletteIndex |= 0b_0001;
      if (Row1.TestBit(column)) paletteIndex |= 0b_0010;
      if (Row2.TestBit(column)) paletteIndex |= 0b_0100;
      if (Row3.TestBit(column)) paletteIndex |= 0b_1000;
      return paletteIndex;
    }
  }

  public readonly struct Tile
  {
    private readonly ushort Data;
    public Tile(ushort data) => Data = data;
    public int PatternIndex => Data & 0b_0000_0001_1111_1111;
    public bool HorizontalFlip => Data.TestBit(9);
    public bool VerticalFlip => Data.TestBit(10);
    public bool UseSpritePalette => Data.TestBit(11);
    public bool HighPriotity => Data.TestBit(12);
  }
}