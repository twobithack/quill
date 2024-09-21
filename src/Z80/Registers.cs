using Quill.Extensions;
using static Quill.Z80.Opcodes;

namespace Quill.Z80
{
  public partial class CPU
  {
    private ushort _pc;
    private ushort _sp;
    private ushort _ix;
    private ushort _iy;

    private byte _a;
    private byte _b;
    private byte _c;
    private byte _d;
    private byte _e;
    private byte _f;
    private byte _h;
    private byte _l;
    private byte _i;
    private byte _r;
    private byte _aS;
    private byte _bS;
    private byte _cS;
    private byte _dS;
    private byte _eS;
    private byte _fS;
    private byte _hS;
    private byte _lS;

    private bool _iff1;
    private bool _iff2;

    private Opcode _instruction;

    private ushort _af
    {
      get => _a.Append(_f);
      set
      {
        _a = value.GetHighByte();
        _f = value.GetLowByte();
      }
    }

    private ushort _bc
    {
      get => _b.Append(_c);
      set
      {
        _b = value.GetHighByte();
        _c = value.GetLowByte();
      }
    }

    private ushort _de
    {
      get => _d.Append(_e);
      set
      {
        _d = value.GetHighByte();
        _e = value.GetLowByte();
      }
    }

    private ushort _hl
    {
      get => _h.Append(_l);
      set
      {
        _h = value.GetHighByte();
        _l = value.GetLowByte();
      }
    }

    private byte ReadRegister(Operand register) => register switch
    {
      Operand.A => _a,
      Operand.B => _b,
      Operand.C => _c,
      Operand.D => _d,
      Operand.E => _e,
      Operand.F => _f,
      Operand.H => _h,
      Operand.L => _l,
      _ => throw new InvalidOperationException()
    };

    private ushort ReadRegisterPair(Operand register) => register switch
    {
      Operand.AF => _af,
      Operand.BC => _bc,
      Operand.DE => _de,
      Operand.HL => _hl,
      Operand.IX => _ix,
      Operand.IY => _iy,
      Operand.PC => _pc,
      Operand.SP => _sp,
      _ => throw new InvalidOperationException()
    };

    private bool _sign
    {
      get => _flags.HasFlag(Flags.Sign);
      set => SetFlag(Flags.Sign, value);
    }

    private bool _zero
    {
      get => _flags.HasFlag(Flags.Zero);
      set => SetFlag(Flags.Zero, value);
    }

    private bool _halfcarry
    {
      get => _flags.HasFlag(Flags.Halfcarry);
      set => SetFlag(Flags.Halfcarry, value);
    }

    private bool _parity
    {
      get => _flags.HasFlag(Flags.Parity);
      set => SetFlag(Flags.Parity, value);
    }

    private bool _overflow
    {
      get => _flags.HasFlag(Flags.Parity);
      set => SetFlag(Flags.Parity, value);
    }

    private bool _negative
    {
      get => _flags.HasFlag(Flags.Negative);
      set => SetFlag(Flags.Negative, value);
    }

    private bool _carry
    {
      get => _flags.HasFlag(Flags.Carry);
      set => SetFlag(Flags.Carry, value);
    }

    private void SetFlag(Flags flag, bool value) => _flags = value
                                                          ? _flags | flag 
                                                          : _flags & ~flag;
    
    private Flags _flags
    {
      get => (Flags) _f;
      set => _f = (byte)value;
    }

    private void ResetRegisters()
    { 
      _a = 0;
      _b = 0;
      _c = 0;
      _d = 0;
      _e = 0;
      _f = 0;
      _h = 0;
      _l = 0;
      _i = 0;
      _r = 0;
      _aS = 0;
      _bS = 0;
      _cS = 0;
      _dS = 0;
      _eS = 0;
      _fS = 0;
      _hS = 0;
      _lS = 0;
      _pc = 0;
      _sp = 0;
      _ix = 0;
      _iy = 0;
      _iff1 = false;
      _iff2 = false;
      _instruction = new Opcode();
    }

    public string DumpRegisters()
    {
      return $"╒══════════╤═══════════╤═══════════╤═══════════╤═══════════╕\r\n" +
             $"│Registers │ AF: {_af.ToHex()} │ BC: {_bc.ToHex()} │ DE: {_de.ToHex()} │ HL: {_hl.ToHex()} │\r\n" +
             $"│          │ IX: {_ix.ToHex()} │ IY: {_iy.ToHex()} │ PC: {_pc.ToHex()} │ SP: {_sp.ToHex()} │\r\n" +
             $"╘══════════╧═══════════╧═══════════╧═══════════╧═══════════╛\r\n";
    }
  }
}