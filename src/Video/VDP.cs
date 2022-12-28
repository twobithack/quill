using Quill.Common;
using Quill.Video.Definitions;
using System.Runtime.CompilerServices;

namespace Quill.Video;

unsafe public ref struct VDP
{
  private const ushort VRAM_SIZE = 0x4000;
  private const byte CRAM_SIZE = 0x20;
  private const byte REGISTER_COUNT = 11;

  public bool IRQ = false;
  public byte VCounter = 0x00;
  public byte HCounter = 0x00;

  private readonly byte[] _vram = new byte[VRAM_SIZE];
  private readonly byte[] _cram = new byte[CRAM_SIZE];
  private readonly byte[] _registers = new byte[REGISTER_COUNT];
  private ushort _controlWord = 0x0000;
  private byte _dataBuffer = 0x00;
  private byte _status = 0x00;
  private bool _writePending = false;

  public VDP() {}

  private ushort _address => (ushort)(_controlWord & 0b_0011_1111_1111_1111);
  private ControlCode _controlCode => (ControlCode)(_controlWord >> 14);
  private bool _vsyncEnabled => _registers[1].TestBit(5);
  private bool _vsyncPending => _status.MSB();

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public byte ReadStatus()
  {
    var status = _status;
    _status = 0x00;
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
      if (register > 10)
        return;

      _registers[register] = _controlWord.LowByte();

      if (register == 0 &&
          _vsyncEnabled && 
          _vsyncPending)
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
  private void IncrementAddress()
  {
    if (_address == VRAM_SIZE - 1)
      _controlWord &= 0b_1100_0000_0000_0000;
    else
      _controlWord++;
  }

  public override string ToString()
  {
    var state = $"VDP: {_controlCode.ToString()}\r\n"; 

    for (var register = 0; register < 11; register++)
      state += $"R{register}:{_registers[register].ToHex()} ";

    return state;
  }
}