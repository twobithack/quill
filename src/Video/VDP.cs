using Quill.Common;
using Quill.Video.Definitions;
using System;
using System.Collections.Generic;
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
  private double _cycleCounter = 0d;
  private ushort _hCounter = 0x0000;
  private bool _vCounterJumped = false;
  private byte _lineInterrupt = 0x00;
  private byte _hScroll = 0x00;
  private byte _vScroll = 0x00;
  private byte[] _framebuffer;
  private byte[] _lastFrame;

  public VDP()
  {
    _framebuffer = new byte[FRAMEBUFFER_SIZE];
    _lastFrame = new byte[FRAMEBUFFER_SIZE];
  }

  public byte HCounter => (byte)(_hCounter >> 1);

  private ushort _address => (ushort)(_controlWord & 0b_0011_1111_1111_1111);
  private ControlCode _controlCode => (ControlCode)(_controlWord >> 14);

  // TODO: derive from display mode
  private byte _vCountActive = 191;
  private byte _xResolution = 255;
  private byte _vJumpFrom = 0xDA; 
  private byte _vJumpTo = 0xD5;
  private bool _vSyncPending
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
  
  private bool _shiftX => TestRegisterBit(0x0, 3);
  private bool _lineInterruptEnabled => TestRegisterBit(0x0, 4);
  private bool _maskLeftBorder => TestRegisterBit(0x0, 5);
  private bool _hScrollLock => TestRegisterBit(0x0, 6);
  private bool _vScrollLock => TestRegisterBit(0x0, 7);
  private bool _zoomSprites => TestRegisterBit(0x1, 0);
  private bool _stretchSprites => TestRegisterBit(0x1, 1);
  private bool _vSyncEnabled => TestRegisterBit(0x1, 5);
  private bool _screenEnabled => TestRegisterBit(0x1, 6);
  private bool _useSecondPatternTable => TestRegisterBit(0x6, 2);

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

    if (_controlCode == ControlCode.WriteVRAM)
      _dataBuffer = _vram[_address];
    else if (_controlCode == ControlCode.WriteRegister)
    {
      var register = _controlWord.HighByte().LowNibble();
      if (register >= REGISTER_COUNT)
        return;

      _registers[register] = _controlWord.LowByte();

      if (register == 0x0 &&
          _vSyncEnabled && 
          _vSyncPending)
        IRQ = true;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public byte ReadData()
  {
    var data = _dataBuffer;
    _controlWritePending = false;
    _dataBuffer = _vram[_address];
    IncrementAddress();
    return data;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void WriteData(byte value)
  {
    _controlWritePending = false;
    _dataBuffer = value;

    if (_controlCode == ControlCode.WriteCRAM)
    {
      var index = _controlWord & 0b_0001_1111;
      _cram[index] = value;
    }
    else
      _vram[_address] = value;

    IncrementAddress();
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void AcknowledgeIRQ() => IRQ = false;

  public void Update(double cyclesElapsed)
  {
    IRQ = false;

    _cycleCounter += cyclesElapsed;
    var cyclesThisUpdate = (int)_cycleCounter;
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
        _vSyncPending = true;
        RenderFrame();
      }

      if (VCounter > _vCountActive)
        _lineInterrupt = _registers[0xA];

      if (VCounter >= _vCountActive)
      {
        _vScroll = _registers[0x9];
      }
      else
      {
        if (_screenEnabled)
          RenderScanline();
      }

      if (VCounter <= _vCountActive)
      {
        if (_lineInterrupt == 0x00)
        {
          _lineInterrupt = _registers[0xA];
          if (_lineInterruptEnabled)
            IRQ = true;
        }
        else
          _lineInterrupt--;
      }
    }

    if (_vSyncEnabled && _vSyncPending)
      IRQ = true;

    _cycleCounter -= cyclesThisUpdate;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public byte[] ReadFramebuffer()
  {
    lock (_lastFrame)
      return _lastFrame;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RenderFrame()
  {
    lock (_lastFrame)
      Array.Copy(_framebuffer, _lastFrame, FRAMEBUFFER_SIZE);
    Array.Fill<byte>(_framebuffer, 0x00);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RenderScanline()
  {
    var spriteHeight = (_stretchSprites || _zoomSprites) ? 16 : 8;
    var spriteCount = 0;

    var satBaseAddress = GetSpriteAttributeTableAddress();
    for (int sprite = 0; sprite < 64; sprite++)
    {
      ushort y = _vram[satBaseAddress + sprite];
      if (y == DISABLE_SPRITES)
        return;

      if (y >= 0xD0)
        y -= 0x100;

      y++;
      if (y > VCounter ||
          y + spriteHeight <= VCounter)
        continue;

      spriteCount++;
      if (spriteCount > 8)
      {
        _status |= Status.Overflow;
        return;
      }

      var offset = 0x80 + (sprite * 2);
      var x = _vram[satBaseAddress + offset];
      ushort patternIndex = _vram[satBaseAddress + offset + 1];

      if (_shiftX)
        x -= 8;

      if (_useSecondPatternTable)
        patternIndex += 0x100;

      if (_stretchSprites && y < VCounter + 9)
        patternIndex &= 0b_1111_1111_1111_1110;

      var patternAddress = patternIndex * 32;
      patternAddress += (VCounter - y) * 4;

      var pattern0 = _vram[patternAddress];
      var pattern1 = _vram[patternAddress + 1];
      var pattern2 = _vram[patternAddress + 2];
      var pattern3 = _vram[patternAddress + 3];
      
      for (byte i = 0, col = 7; i < 8; i++, col--)
      {
        if (x + i >= _xResolution)
          break;

        if (_maskLeftBorder && x + i < 8)
          continue;

        var index = GetFramebufferIndex(x + i, VCounter);
        if (_framebuffer[index + 3] != 0x00)
        {
          _status |= Status.Collision;
          continue;
        }

        byte palette = 0x00;
        if (pattern0.TestBit(col))
          palette++;
        if (pattern1.TestBit(col))
          palette |= 0b_0010;
        if (pattern2.TestBit(col))
          palette |= 0b_0100;
        if (pattern3.TestBit(col))
          palette |= 0b_1000;

        if (palette == 0x00)
          continue;

        var color = _cram[palette + 16];
        _framebuffer[index] = (byte)((color & 0b_0011) * 85);
        color >>= 2;
        _framebuffer[index + 1] = (byte)((color & 0b_0011) * 85);
        color >>= 2;
        _framebuffer[index + 2] = (byte)((color & 0b_0011) * 85);
        _framebuffer[index + 3] = 0xFF;
      }
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void GenerateNoise()
  {
    var random = new Random();
    for (int index = 0; index < _framebuffer.Length; index = index + 4)
      _framebuffer[index] = _framebuffer[index+1] = _framebuffer[index+2] = (byte)(byte.MaxValue * random.NextSingle());
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void IncrementAddress()
  {
    if (_address == VRAM_SIZE - 1)
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
  private static int GetFramebufferIndex(int x, int y) => (x + (y * 256)) * 4;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private ushort GetSpriteAttributeTableAddress()
  {
    var address = _registers[0x5] & 0b_0111_1110;
    return (ushort)(address << 7);
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
    var state = $"VDP | Control: {_controlCode} | Address: {_address.ToHex()} | SAT Address: {GetSpriteAttributeTableAddress().ToHex()}\r\n"; 

    for (byte register = 0; register < REGISTER_COUNT; register++)
      state += $"R{register.ToHex()}:{_registers[register].ToHex()} ";

    return state;
  }
}