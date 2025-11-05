using Quill.Core;
using Quill.Memory;
using Quill.Sound;
using Quill.Video;

namespace Quill.IO;

public ref struct Bus
{
  #region Fields
  private Mapper _memory;
  private readonly Ports _ports;
  private readonly PSG _psg;
  private readonly VDP _vdp;
  #endregion

  public Bus(Mapper memory, Ports ports, PSG psg, VDP vdp)
  {
    _memory = memory;
    _ports = ports;
    _psg = psg;
    _vdp = vdp;
  }

  #region Properties
  public readonly bool IRQ => _vdp.IRQ;

  public readonly bool NMI
  {
    get => _ports.NMI;
    set => _ports.NMI = value;
  }
  #endregion

  #region Methods
  public readonly byte ReadByte(ushort address) => _memory.ReadByte(address);
  public readonly ushort ReadWord(ushort address) => _memory.ReadWord(address);
  public void WriteByte(ushort address, byte value) => _memory.WriteByte(address, value);
  public void WriteWord(ushort address, ushort word) => _memory.WriteWord(address, word);

  public readonly byte ReadPort(byte port) => port switch
  {
    0x3E => 0xFF,
    0x3F => 0xFF,
    0x7E => _vdp.VCounter,
    0x7F => _vdp.HCounter,
    0xBE => _vdp.ReadData(),
    0xBF or 0xBD => _vdp.ReadStatus(),
    0xDC or 0xC0 => _ports.ReadPortA(),
    0xDD or 0xC1 => _ports.ReadPortB(),
    byte mirror when mirror < 0x3E => 0xFF,
    byte mirror when mirror > 0x3F &&
                     mirror < 0x80 &&
                    (mirror & 1) == 0 => _vdp.VCounter,
    byte mirror when mirror > 0x3F &&
                     mirror < 0x80 &&
                    (mirror & 1) != 0 => _vdp.HCounter,
    byte mirror when mirror > 0x7F &&
                     mirror < 0xC0 &&
                    (mirror & 1) == 0 => _vdp.ReadData(),
    byte mirror when mirror > 0x7F &&
                     mirror < 0xC0 &&
                    (mirror & 1) != 0 => _vdp.ReadStatus(),
    byte mirror when mirror > 0xC0 &&
                    (mirror & 1) == 0 => _ports.ReadPortA(),
    byte mirror when mirror > 0xC1 &&
                    (mirror & 1) != 0 => _ports.ReadPortB(),
    _ => 0xFF
  };

  public readonly void WritePort(byte port, byte value)
  {
    switch (port)
    {
      case 0x7E:
      case 0x7F:
        _psg.WriteData(value);
        return;

      case 0xBD:
      case 0xBF:
        _vdp.WriteControl(value);
        return;

      case 0xBE:
        _vdp.WriteData(value);
        return;

      case 0x3E:
      case byte mirror when mirror < 0x3E &&
                           (mirror & 1) == 0:
        // Memory controller
        return;

      case 0x3F:
      case byte mirror when mirror < 0x3E &&
                           (mirror & 1) != 0:
        _ports.WriteControl(value);
        return;

      case byte mirror when mirror > 0x3F &&
                            mirror < 0x80:
        _psg.WriteData(value);
        return;

      case byte mirror when mirror > 0x7F &&
                            mirror < 0xC0 &&
                           (mirror & 1) != 0:
        _vdp.WriteControl(value);
        return;

      case byte mirror when mirror > 0x7F &&
                            mirror < 0xC0 &&
                           (mirror & 1) == 0:
        _vdp.WriteData(value);
        return;
    }
  }

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