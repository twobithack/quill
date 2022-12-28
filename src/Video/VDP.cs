using System.Runtime.CompilerServices;
using Quill.Common;

namespace Quill.Video;

unsafe public ref struct VDP
{
  private const byte VRAM_READ = 0x0;
  private const byte VRAM_WRITE = 0x1;
  private const byte REGISTER_WRITE = 0x2;
  private const byte CRAM_WRITE = 0x3;
  private const byte CRAM_SIZE = 0x20;
  private const ushort VRAM_SIZE = 0x4000;

  public bool IRQ = false;
  public byte VCounter = 0x00;
  public byte HCounter = 0x00;

  private readonly byte[] _vram = new byte[VRAM_SIZE];
  private readonly byte[] _cram = new byte[CRAM_SIZE];
  private readonly byte[] _registers = new byte[11];
  private ushort _controlWord = 0x0000;
  private byte _dataBuffer = 0x00;
  private byte _status = 0x00;
  private bool _writePending = false;

  public VDP() {}

  private ushort _address => (ushort)(_controlWord & 0b_0011_1111_1111_1111);
  private byte _controlCode => (byte)(_controlWord >> 14);
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

    if (_controlCode == VRAM_WRITE)
    {
      _dataBuffer = _vram[_address];
      IncrementAddress();
    }
    else if (_controlCode == REGISTER_WRITE)
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

    if (_controlCode == CRAM_WRITE)
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
    var state = "VDP: " + _controlCode switch
    {
      VRAM_READ => "VRAM Read",
      VRAM_WRITE => "VRAM Write",
      REGISTER_WRITE => "Register Write",
      CRAM_WRITE => "CRAM Write"
    };

    for (var register = 0; register < 11; register++)
      state += $"R{register}:{_registers[register].ToHex()} ";

    return state;
  }
}