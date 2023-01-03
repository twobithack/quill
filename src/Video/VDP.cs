using Quill.Common;
using Quill.Video.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Quill.Video;

unsafe public class VDP
{
  private const int FRAMEBUFFER_SIZE = 0x30000;
  private const ushort VRAM_SIZE = 0x4000;
  private const byte CRAM_SIZE = 0x20;
  private const byte REGISTER_COUNT = 0xB;
  private const ushort HCOUNTER_MAX = 685;
  private const byte DISABLE_SPRITES = 0xD0;

  public bool IRQ = false;
  public byte VCounter = 0x00;

  private readonly byte[] _vram = new byte[VRAM_SIZE];
  private readonly byte[] _cram = new byte[CRAM_SIZE];
  private readonly byte[] _registers = new byte[REGISTER_COUNT];
  private ushort _controlWord = 0x0000;
  private byte _dataBuffer = 0x00;
  private Status _status = 0x00;
  private bool _controlWritePending = false;
  private double _cycleCount = 0d;
  private ushort _hCounter = 0x0000;
  private bool _vCounterJumped = false;
  private byte _lineInterrupt = 0x00;
  private byte _hScroll = 0x00;
  private byte _vScroll = 0x00;
  private bool _frameQueued = false;
  private readonly byte[] _framebuffer;
  private readonly byte[] _renderbuffer;

  // TODO: derive from display mode
  private readonly byte _hResolution = 255;
  private readonly byte _vCountActive = 192;
  private readonly byte _vJumpFrom = 0xDA;
  private readonly byte _vJumpTo = 0xD5;

  public VDP()
  {
    _framebuffer = new byte[FRAMEBUFFER_SIZE];
    _renderbuffer = new byte[FRAMEBUFFER_SIZE];
  }

  public byte HCounter => (byte)(_hCounter >> 1);
  private ushort Address => (ushort)(_controlWord & 0b_0011_1111_1111_1111);
  private ControlCode ControlCode => (ControlCode)(_controlWord >> 14);

  private bool VSyncPending
  {
    get => _status.HasFlag(Status.VSync);
    set
    {
      if (value)
        _status |= Status.VSync;
      else
        _status &= ~Status.VSync;
    } 
  } 
  
  private bool ShiftX => TestRegisterBit(0x0, 3);
  private bool LineInterruptEnabled => TestRegisterBit(0x0, 4);
  private bool MaskLeftBorder => TestRegisterBit(0x0, 5);
  private bool HScrollLock => TestRegisterBit(0x0, 6);
  private bool VScrollLock => TestRegisterBit(0x0, 7);
  private bool ZoomSprites => TestRegisterBit(0x1, 0);
  private bool StretchSprites => TestRegisterBit(0x1, 1);
  private bool VSyncEnabled => TestRegisterBit(0x1, 5);
  private bool DisplayEnabled => TestRegisterBit(0x1, 6);
  private bool UseSecondPatternTable => TestRegisterBit(0x6, 2);

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
      _cram[index] = value;
    }
    else
      _vram[Address] = value;

    IncrementAddress();
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void AcknowledgeIRQ() => IRQ = false;

  public void Update(double cyclesElapsed)
  {
    IRQ = false;

    _cycleCount += cyclesElapsed;
    var cyclesThisUpdate = (int)_cycleCount;
    _hCounter += (ushort)(cyclesThisUpdate * 2);

    if (_hCounter >= HCOUNTER_MAX)
    {
      _hCounter %= HCOUNTER_MAX;
      VCounter++;

      if (VCounter == byte.MinValue)
      {
        _vCounterJumped = false;
      }
      else if (!_vCounterJumped && VCounter == _vJumpFrom)
      {
        VCounter = _vJumpTo;
        _vCounterJumped = true;
      }
      else if (VCounter == _vCountActive)
      {
        VSyncPending = true;
        RenderFrame();
      }

      if (VCounter > _vCountActive)
        _lineInterrupt = _registers[0xA];
      else if (_lineInterrupt == 0x00)
      {
        _lineInterrupt = _registers[0xA];
        if (LineInterruptEnabled)
          IRQ = true;
      }
      else
        _lineInterrupt--;
     

      if (VCounter >= _vCountActive)
      {
        _hScroll = _registers[0x8];
        _vScroll = _registers[0x9];
      }
      else if (DisplayEnabled)
        RenderScanline();
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

    for (var pixelIndex = 0; pixelIndex < FRAMEBUFFER_SIZE; pixelIndex += 4)
    {
      var color = _cram[_registers[0x7] & 0b_0011];
      _renderbuffer[pixelIndex] = (byte)((color & 0b_0011) * 85);
      color >>= 2;
      _renderbuffer[pixelIndex + 1] = (byte)((color & 0b_0011) * 85);
      color >>= 2;
      _renderbuffer[pixelIndex + 2] = (byte)((color & 0b_0011) * 85);
      _renderbuffer[pixelIndex + 3] = 0x00;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RenderScanline()
  {
    RasterizeSprites();
    RasterizeBackground();
  }

  private void RasterizeSprites()
  {
    var spriteHeight = (StretchSprites || ZoomSprites) ? 16 : 8;
    var spriteCount = 0;

    var baseAddress = GetSpriteAttributeTableAddress();
    for (int sprite = 0; sprite < 64; sprite++)
    {
      ushort y = _vram[baseAddress + sprite];
      if (y == DISABLE_SPRITES)
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
      var x = _vram[baseAddress + offset];
      ushort patternIndex = _vram[baseAddress + offset + 1];

      if (ShiftX)
        x -= 8;

      if (UseSecondPatternTable)
        patternIndex += 0x100;

      if (StretchSprites && y < VCounter + 9)
        patternIndex &= 0b_1111_1111_1111_1110;

      var patternAddress = patternIndex * 32;
      patternAddress += (VCounter - y) * 4;

      var pattern0 = _vram[patternAddress];
      var pattern1 = _vram[patternAddress + 1];
      var pattern2 = _vram[patternAddress + 2];
      var pattern3 = _vram[patternAddress + 3];

      var spriteEnd = x + 8;
      for (byte i = 7; x < spriteEnd; x++, i--)
      {
        if (x >= _hResolution)
          break;

        if (x < 8 && MaskLeftBorder)
          continue;

        var pixelIndex = GetPixelIndex(x, VCounter);
        if (_renderbuffer[pixelIndex + 3] != 0x00)
        {
          _status |= Status.Collision;
          continue;
        }

        byte palette = 0x00;
        if (pattern0.TestBit(i)) palette |= 0b_0001;
        if (pattern1.TestBit(i)) palette |= 0b_0010;
        if (pattern2.TestBit(i)) palette |= 0b_0100;
        if (pattern3.TestBit(i)) palette |= 0b_1000;

        if (palette == 0x00)
          continue;

        var color = _cram[palette + 16];
        _renderbuffer[pixelIndex] = (byte)((color & 0b_0011) * 85);
        color >>= 2;
        _renderbuffer[pixelIndex + 1] = (byte)((color & 0b_0011) * 85);
        color >>= 2;
        _renderbuffer[pixelIndex + 2] = (byte)((color & 0b_0011) * 85);
        _renderbuffer[pixelIndex + 3] = 0xFF;
      }
    }
  }

  private void RasterizeBackground()
  {
    var baseAddress = GetNameTableAddress();
    var sourceRow = VCounter + _vScroll;
    if (sourceRow > 223) sourceRow -= 223;
    var rowOffset = sourceRow % 8;
    sourceRow >>= 3;

    for (int targetColumn = 0; targetColumn < 32; targetColumn++)
    {
      var sourceColumn = targetColumn - (_hScroll / 8);
      sourceColumn %= 32;
      if (sourceColumn < 0) 
        sourceColumn += 32;

      var tileAddress = baseAddress + 
                        (sourceRow * 64) + 
                        (sourceColumn * 2);
                        
      var tileData = _vram[tileAddress + 1].Concat(_vram[tileAddress]);
      var hFlip = tileData.TestBit(9);
      var vFlip = tileData.TestBit(10);
      var useSpritePalette = tileData.TestBit(11);
      var highPriority = tileData.TestBit(12);

      for (int i = 0; i < 8; i++)
      {
        var x = (sourceColumn * 8) + _hScroll + i;
        x %= (_hResolution + 1);

        if (x > _hResolution)
          break;

        if (x < 8 && MaskLeftBorder)
          continue;

        var pixelIndex = GetPixelIndex(x, VCounter);
        if (!highPriority &&
            _renderbuffer[pixelIndex + 3] != 0x00)
          continue;

        var patternIndex = tileData & 0b_0000_0001_1111_1111;
        var patternAddress = (patternIndex * 32) + (rowOffset * 4);
        var pattern0 = _vram[patternAddress];
        var pattern1 = _vram[patternAddress + 1];
        var pattern2 = _vram[patternAddress + 2];
        var pattern3 = _vram[patternAddress + 3];


        byte palette = 0x00;
        if (pattern0.TestBit(7 - i)) palette |= 0b_0001;
        if (pattern1.TestBit(7 - i)) palette |= 0b_0010;
        if (pattern2.TestBit(7 - i)) palette |= 0b_0100;
        if (pattern3.TestBit(7 - i)) palette |= 0b_1000;

        if (palette == 0x00)
          continue;

        if (useSpritePalette)
          palette += 16;

        var color = _cram[palette];
        _renderbuffer[pixelIndex] = (byte)((color & 0b_0011) * 85);
        color >>= 2;
        _renderbuffer[pixelIndex + 1] = (byte)((color & 0b_0011) * 85);
        color >>= 2;
        _renderbuffer[pixelIndex + 2] = (byte)((color & 0b_0011) * 85);
        _renderbuffer[pixelIndex + 3] = 0xFF;
      }
    }
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
}