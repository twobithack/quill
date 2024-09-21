using System.Runtime.CompilerServices;
using Quill.Definitions;
using static Quill.Definitions.Opcodes;
using Quill.Extensions;

namespace Quill;

public unsafe ref partial struct CPU
{
  private Flags _flags = Flags.None;
  private Opcode _instruction = new Opcode();
  private Memory _memory;
  private VDP _vdp;

  private bool _halt = false;
  private bool _iff1 = true;
  private bool _iff2 = true;

  private byte _a = 0x00;
  private byte _b = 0x00;
  private byte _c = 0x00;
  private byte _d = 0x00;
  private byte _e = 0x00;
  private byte _h = 0x00;
  private byte _l = 0x00;
  private byte _r = 0x00;

  private ushort _pc = 0x0000;
  private ushort _sp = 0x0000;
  private ushort _ix = 0x0000;
  private ushort _iy = 0x0000;

  private ushort _afShadow = 0x0000;
  private ushort _bcShadow = 0x0000;
  private ushort _deShadow = 0x0000;
  private ushort _hlShadow = 0x0000;
  private ushort _memPtr = 0x0000;

  private ulong _cycleCount = 0;

  private byte _ixh
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => _ix.HighByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => _ix = value.Concat(_ixl);
  }

  private byte _ixl
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => _ix.LowByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => _ix = _ixh.Concat(value);
  }

  private byte _iyh
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => _iy.HighByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => _iy = value.Concat(_iyl);
  }

  private byte _iyl
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => _iy.LowByte();

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
      _a = value.HighByte();
      _flags = (Flags)value.LowByte();
    }
  }

  private ushort _bc
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => _b.Concat(_c);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set
    {
      _b = value.HighByte();
      _c = value.LowByte();
    }
  }

  private ushort _de
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => _d.Concat(_e);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set
    {
      _d = value.HighByte();
      _e = value.LowByte();
    }
  }

  private ushort _hl
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => _h.Concat(_l);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set
    {
      _h = value.HighByte();
      _l = value.LowByte();
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
    return $"╒══════════╤══════════╤══════════╤══════════╤═══════════╕\r\n" +
            $"│ PC: {_pc.ToHex()} │ SP: {_sp.ToHex()} │ IX: {_ix.ToHex()} │ IY: {_iy.ToHex()} │ R: {_r.ToHex()}     │\r\n" +
            $"│ AF: {_af.ToHex()} │ BC: {_bc.ToHex()} │ DE: {_de.ToHex()} │ HL: {_hl.ToHex()} │ IFF1: {_iff1.ToBit()}   │\r\n" +
            $"│     {_afShadow.ToHex()} │     {_bcShadow.ToHex()} │     {_deShadow.ToHex()} │     {_hlShadow.ToHex()} │ IFF2: {_iff2.ToBit()}   │\r\n" +
            $"╘══════════╧══════════╧══════════╧══════════╧═══════════╛\r\n";
  }
}