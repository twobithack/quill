using System.Runtime.CompilerServices;
using Quill.Core;
using Quill.Memory;
using Quill.Sound;
using Quill.Video;

namespace Quill.IO;

public ref struct Bus
{
  #region Constants
  private const byte PORT_MASK = 0b_1100_0001;
  #endregion

  #region Fields
  private Mapper _memory;
  private readonly Ports _io;
  private readonly PSG _psg;
  private readonly VDP _vdp;
  #endregion

  public Bus(Mapper memory, Ports ports, PSG psg, VDP vdp)
  {
    _memory = memory;
    _io = ports;
    _psg = psg;
    _vdp = vdp;
  }

  #region Properties
  public readonly bool IRQ => _vdp.IRQ;

  public readonly bool NMI
  {
    get => _io.NMI;
    set => _io.NMI = value;
  }
  #endregion

  #region Methods
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public readonly byte ReadByte(ushort address) => _memory.ReadByte(address);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public readonly ushort ReadWord(ushort address) => _memory.ReadWord(address);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void WriteByte(ushort address, byte value) => _memory.WriteByte(address, value);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void WriteWord(ushort address, ushort word) => _memory.WriteWord(address, word);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public readonly byte ReadPort(byte port)
  {
    return (port & PORT_MASK) switch
    {
      // Ports 0x00 - 0x3F
      0b_0000_0000 => 0xFF,
      0b_0000_0001 => 0xFF,

      // Ports 0x40 - 0x7F
      0b_0100_0000 => _vdp.VCounter,
      0b_0100_0001 => _vdp.HCounter,

      // Ports 0x80 - 0xBF
      0b_1000_0000 => _vdp.ReadData(),
      0b_1000_0001 => _vdp.ReadStatus(),

      // Ports 0xC0 - 0xFF
      0b_1100_0000 => _io.ReadPortA(),
      0b_1100_0001 => _io.ReadPortB()
    };
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public readonly void WritePort(byte port, byte value)
  {
    switch (port & PORT_MASK)
    {
      // Ports 0x00 - 0x3F
      case 0b_0000_0000: _memory.WriteControl(value); return;
      case 0b_0000_0001: _io.WriteControl(value);     return;

      // Ports 0x40 - 0x7F
      case 0b_0100_0000: _psg.WriteData(value);       return;
      case 0b_0100_0001: _psg.WriteData(value);       return;

      // Ports 0x80 - 0xBF
      case 0b_1000_0000: _vdp.WriteData(value);       return;
      case 0b_1000_0001: _vdp.WriteControl(value);    return;

      // Ports 0xC0 - 0xFF
      case 0b_1100_0000: return;
      case 0b_1100_0001: return;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public readonly void Step(int cycles)
  {
    _psg.Step(cycles);
    _vdp.Step(cycles);
  }

  public void LoadState(Snapshot state)
  {
    _memory.LoadState(state);
    _psg.LoadState(state);
    _vdp.LoadState(state);
  }

  public readonly void SaveState(Snapshot state)
  {
    _memory.SaveState(state);
    _psg.SaveState(state);
    _vdp.SaveState(state);
  }
  #endregion
}