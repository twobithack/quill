using System.Runtime.CompilerServices;
using Quill.Extensions;
using static Quill.Z80.Opcodes;

namespace Quill.Z80
{
  public partial class CPU
  {
    private Flags _flags;

    private ushort _pc;
    private ushort _sp;
    private ushort _ix;
    private ushort _iy;

    private byte _a;
    private byte _b;
    private byte _c;
    private byte _d;
    private byte _e;
    private byte _h;
    private byte _l;
    private byte _aShadow;
    private byte _bShadow;
    private byte _cShadow;
    private byte _dShadow;
    private byte _eShadow;
    private byte _fShadow;
    private byte _hShadow;
    private byte _lShadow;
    
    private byte _i;
    private byte _r;
    private bool _iff1;
    private bool _iff2;

    private Opcode _instruction;

    private ushort _af
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => _a.Concat((byte)_flags);

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set
      {
        _a = value.GetHighByte();
        _flags = (Flags)value.GetLowByte();
      }
    }

    private ushort _bc
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => _b.Concat(_c);

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set
      {
        _b = value.GetHighByte();
        _c = value.GetLowByte();
      }
    }

    private ushort _de
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => _d.Concat(_e);

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set
      {
        _d = value.GetHighByte();
        _e = value.GetLowByte();
      }
    }

    private ushort _hl
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => _h.Concat(_l);

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set
      {
        _h = value.GetHighByte();
        _l = value.GetLowByte();
      }
    }

    private ushort _afShadow
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => _aShadow.Concat(_fShadow);

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set
      {
        _aShadow = value.GetHighByte();
        _fShadow = value.GetLowByte();
      }
    }

    private ushort _bcShadow
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => _bShadow.Concat(_cShadow);

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set
      {
        _bShadow = value.GetHighByte();
        _cShadow = value.GetLowByte();
      }
    }

    private ushort _deShadow
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => _dShadow.Concat(_eShadow);

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set
      {
        _dShadow = value.GetHighByte();
        _eShadow = value.GetLowByte();
      }
    }

    private ushort _hlShadow
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => _hShadow.Concat(_lShadow);

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set
      {
        _hShadow = value.GetHighByte();
        _lShadow = value.GetLowByte();
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte ReadRegister(Operand register) => register switch
    {
      Operand.A => _a,
      Operand.B => _b,
      Operand.C => _c,
      Operand.D => _d,
      Operand.E => _e,
      Operand.H => _h,
      Operand.L => _l,
      _ => throw new InvalidOperationException($"Invalid source: {register}")
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort ReadRegisterPair(Operand registers) => registers switch
    {
      Operand.AF => _af,
      Operand.BC => _bc,
      Operand.DE => _de,
      Operand.HL => _hl,
      Operand.IX => _ix,
      Operand.IY => _iy,
      Operand.PC => _pc,
      Operand.SP => _sp,
      _ => throw new InvalidOperationException($"Invalid source: {registers}")
    };

    private bool _sign
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => _flags.HasFlag(Flags.Sign);

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => SetFlag(Flags.Sign, value);
    }

    private bool _zero
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => _flags.HasFlag(Flags.Zero);

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => SetFlag(Flags.Zero, value);
    }

    private bool _halfcarry
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => _flags.HasFlag(Flags.Halfcarry);

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => SetFlag(Flags.Halfcarry, value);
    }

    private bool _parity
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => _flags.HasFlag(Flags.Parity);

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => SetFlag(Flags.Parity, value);
    }

    private bool _overflow
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => _flags.HasFlag(Flags.Parity);

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => SetFlag(Flags.Parity, value);
    }

    private bool _negative
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => _flags.HasFlag(Flags.Negative);

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => SetFlag(Flags.Negative, value);
    }

    private bool _carry
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => _flags.HasFlag(Flags.Carry);

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => SetFlag(Flags.Carry, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetFlag(Flags flag, bool value) => _flags = value
                                                           ? _flags | flag 
                                                           : _flags & ~flag;

    private void ResetRegisters()
    { 
      _pc = 0x0000;
      _sp = 0xFFFF;
      _ix = 0xFFFF;
      _iy = 0xFFFF;
      _af = 0xFFFF;
      _bc = 0xFFFF;
      _de = 0xFFFF;
      _hl = 0xFFFF;
      _afShadow = 0xFFFF;
      _bcShadow = 0xFFFF;
      _deShadow = 0xFFFF;
      _hlShadow = 0xFFFF;
      
      _i = 0x00;
      _r = 0x00;
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