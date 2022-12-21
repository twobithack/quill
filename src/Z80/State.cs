using System.Runtime.CompilerServices;
using Quill.Definitions;
using static Quill.Definitions.Opcodes;
using Quill.Extensions;

namespace Quill
{
  public unsafe ref partial struct CPU
  {
    private Memory _memory;
    private VDP _vdp;
    private Flags _flags;

    private bool _halt;
    private bool _iff1;
    private bool _iff2;

    private byte _a;
    private byte _b;
    private byte _c;
    private byte _d;
    private byte _e;
    private byte _h;
    private byte _l;
    private byte _r;

    private ushort _pc;
    private ushort _sp;
    private ushort _ix;
    private ushort _iy;

    private ushort _afShadow;
    private ushort _bcShadow;
    private ushort _deShadow;
    private ushort _hlShadow;
    private ushort _addressBus;

    private Opcode _instruction;
    private int _cycleCount;

    private byte _ixh
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => _ix.GetHighByte();

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => _ix = value.Concat(_ixl);
    }

    private byte _ixl
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => _ix.GetLowByte();

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => _ix = _ixh.Concat(value);
    }

    private byte _iyh
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => _iy.GetHighByte();

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => _iy = value.Concat(_iyl);
    }

    private byte _iyl
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => _iy.GetLowByte();

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => _iy = _iyh.Concat(value);
    }

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

    public string DumpRegisters()
    {
      return $"╒═══════════╤═══════════╤═══════════╤═══════════╤═══════════╕\r\n" +
             $"│ PC: {_pc.ToHex()} │ SP: {_sp.ToHex()} │ IX: {_ix.ToHex()} │ IY: {_iy.ToHex()} │ R: {_r.ToHex()}     │\r\n" +
             $"│ AF: {_af.ToHex()} │ BC: {_bc.ToHex()} │ DE: {_de.ToHex()} │ HL: {_hl.ToHex()} │ IFF1: {_iff1.ToBit()}   │\r\n" +
             $"│     {_afShadow.ToHex()} │     {_bcShadow.ToHex()} │     {_deShadow.ToHex()} │     {_hlShadow.ToHex()} │ IFF2: {_iff2.ToBit()}   │\r\n" +
             $"╘═══════════╧═══════════╧═══════════╧═══════════╧═══════════╛\r\n";
    }
  }
}