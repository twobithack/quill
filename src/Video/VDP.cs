using Quill.Common;
using Quill.Video.Definitions;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Quill.Video;

unsafe public ref struct VDP
{
  private const ushort VRAM_SIZE = 0x4000;
  private const byte CRAM_SIZE = 0x20;
  private const byte REGISTER_COUNT = 0xB;
  private const ushort HCOUNTER_MAX = 685;

  public bool IRQ = false;
  public byte VCounter = 0x00;

  private readonly byte[] _vram = new byte[VRAM_SIZE];
  private readonly byte[] _cram = new byte[CRAM_SIZE];
  private readonly byte[] _registers = new byte[REGISTER_COUNT];
  private ushort _controlWord = 0x0000;
  private byte _dataBuffer = 0x00;
  private Status _status = 0x00;
  private bool _writePending = false;
  private double _cycleCount = 0d;
  private ushort _hCounter = 0x0000;
  private byte _vCounter = 0x00;
  private bool _vCounterJumped = false;
  private byte _lineInterrupt = 0x00;
  private byte _hScroll = 0x00;
  private byte _vScroll = 0x00;
  private byte[] _framebuffer;

  public VDP()
  {
    _framebuffer = new byte[256 * 192 * 4];
  }

  public byte HCounter => (byte)(_hCounter >> 1);

  private ushort _address => (ushort)(_controlWord & 0b_0011_1111_1111_1111);
  private ControlCode _controlCode => (ControlCode)(_controlWord >> 14);

  // TODO: derive from display mode
  private byte _vCountActive = 191;
  private byte _vJumpFrom = 0xDA; 
  private byte _vJumpTo = 0xD5;
  
  private bool _lineInterruptEnabled => _registers[0].TestBit(4);
  private bool _vSyncEnabled => _registers[1].TestBit(5);
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

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void Update(double systemCyclesElapsed)
  {
    IRQ = _vSyncEnabled && _vSyncPending;

    _cycleCount += (systemCyclesElapsed / 2d);
    var cyclesThisUpdate = (int)_cycleCount;
    var hCount = _hCounter + (cyclesThisUpdate * 2);
    _hCounter = (ushort)(hCount % HCOUNTER_MAX);

    if (hCount >= HCOUNTER_MAX)
    {
      _vCounter++;
      if (_vCounter == byte.MinValue)
      {
        _vCounterJumped = false;
      }
      else if (!_vCounterJumped && _vCounter == _vJumpFrom)
      {
        _vCounter = _vJumpTo;
        _vCounterJumped = true;
      }
      else if (_vCounter == _vCountActive)
      {
        _vSyncPending = true;
      }

      if (_vCounter > _vCountActive)
        _lineInterrupt = _registers[0xA];

      if (_vCounter >= _vCountActive)
      {
        _vScroll = _registers[0x9];
        // TODO: check mode, update resolution
      }
      else
      {
        // TODO: check screen disabled
        RenderFrame();
      }

      if (_vCounter <= _vCountActive)
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

    _cycleCount -= cyclesThisUpdate;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public byte ReadStatus()
  {
    var status = (byte)_status;
    _status = Status.None;
    _writePending = false;
    IRQ = false;
    return status;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void WriteControl(byte value)
  {
    if (!_writePending)
    {
      _controlWord &= 0b_1111_1111_0000_0000;
      _controlWord |= value;
      _writePending = true;
      return;
    }
    
    _controlWord &= 0b_0000_0000_1111_1111;
    _controlWord |= (ushort)(value << 8);
    _writePending = false;

    if (_controlCode == ControlCode.WriteVRAM)
    {
      _dataBuffer = _vram[_address];
      IncrementAddress();
    }
    else if (_controlCode == ControlCode.WriteRegister)
    {
      var register = _controlWord.HighByte().LowNibble();
      if (register >= REGISTER_COUNT)
        return;

      _registers[register] = _controlWord.LowByte();

      if (register == 0 &&
          _vSyncEnabled && 
          _vSyncPending)
        IRQ = true;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public byte ReadData()
  {
    var data = _dataBuffer;
    _writePending = false;
    _dataBuffer = _vram[_address];
    IncrementAddress();
    return data;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void WriteData(byte value)
  {
    _writePending = false;
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
  public void AcknowledgeIRQ()
  {
    IRQ = false;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public byte[] ReadFramebuffer()
  {
    GenerateNoise();
    return _framebuffer;
  }

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

  private void RenderFrame()
  {
    // TODO
  }

  public override string ToString()
  {
    var state = $"VDP: {_controlCode.ToString()}\r\n"; 

    for (var register = 0; register < REGISTER_COUNT; register++)
      state += $"R{register}:{_registers[register].ToHex()} ";

    return state;
  }
}