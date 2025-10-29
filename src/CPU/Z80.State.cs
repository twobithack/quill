using System.Runtime.CompilerServices;

using Quill.Common.Extensions;
using Quill.Core;
using Quill.CPU.Definitions;
using Quill.IO;

namespace Quill.CPU;

unsafe public ref partial struct Z80
{
  #region Fields
  private Bus _bus;

  private Instruction _instruction;
  private ushort? _memPtr = null;
  private ushort _pc = 0x0000;
  private ushort _sp = 0xFFFF;

  private Flags _flags;
  private bool _iff1 = true;
  private bool _iff2 = true;
  private bool _halt = false;
  private bool _eiPending = false;

  private byte _a = 0x00;
  private byte _b = 0x00;
  private byte _c = 0x00;
  private byte _d = 0x00;
  private byte _e = 0x00;
  private byte _h = 0x00;
  private byte _l = 0x00;
  private byte _i = 0x00;
  private byte _r = 0x00;
  private byte _ixh = 0x00;
  private byte _ixl = 0x00;
  private byte _iyh = 0x00;
  private byte _iyl = 0x00;
  private ushort _afShadow = 0x0000;
  private ushort _bcShadow = 0x0000;
  private ushort _deShadow = 0x0000;
  private ushort _hlShadow = 0x0000;
  #endregion

  #region Properties
  public readonly ushort PC => _pc;

  private bool SignFlag
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    readonly get => GetFlag(Flags.Sign);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => SetFlag(Flags.Sign, value);
  }

  private bool ZeroFlag
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    readonly get => GetFlag(Flags.Zero);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => SetFlag(Flags.Zero, value);
  }

  private bool HalfcarryFlag
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    readonly get => GetFlag(Flags.Halfcarry);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => SetFlag(Flags.Halfcarry, value);
  }

  private bool ParityFlag
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    readonly get => GetFlag(Flags.Parity);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => SetFlag(Flags.Parity, value);
  }

  private bool NegativeFlag
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    readonly get => GetFlag(Flags.Negative);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => SetFlag(Flags.Negative, value);
  }

  private bool CarryFlag
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    readonly get => GetFlag(Flags.Carry);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => SetFlag(Flags.Carry, value);
  }

  private ushort AF
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    readonly get => _a.Concat((byte)_flags);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set
    {
      _a = value.HighByte();
      _flags = (Flags)value.LowByte();
    }
  }

  private ushort BC
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    readonly get => _b.Concat(_c);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set
    {
      _b = value.HighByte();
      _c = value.LowByte();
    }
  }

  private ushort DE
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    readonly get => _d.Concat(_e);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set
    {
      _d = value.HighByte();
      _e = value.LowByte();
    }
  }

  private ushort HL
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    readonly get => _h.Concat(_l);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set
    {
      _h = value.HighByte();
      _l = value.LowByte();
    }
  }

  private ushort IX
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    readonly get => _ixh.Concat(_ixl);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set
    {
      _ixh = value.HighByte();
      _ixl = value.LowByte();
    }
  }

  private ushort IY
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    readonly get => _iyh.Concat(_iyl);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set
    {
      _iyh = value.HighByte();
      _iyl = value.LowByte();
    }
  }
  #endregion

  #region Methods
  public void LoadState(Snapshot state)
  {
    if (state == null)
      return;

    AF = state.AF;
    BC = state.BC;
    DE = state.DE;
    HL = state.HL;
    IX = state.IX;
    IY = state.IY;
    _i = state.I;
    _r = state.R;
    _pc = state.PC;
    _sp = state.SP;
    _afShadow = state.AFs;
    _bcShadow = state.BCs;
    _deShadow = state.DEs;
    _hlShadow = state.HLs;
    _halt = state.Halt;
    _iff1 = state.IFF1;
    _iff2 = state.IFF2;
    _bus.LoadState(state);
  }

  public readonly Snapshot SaveState()
  {
    var state = new Snapshot
    {
      AF = AF,
      BC = BC,
      DE = DE,
      HL = HL,
      IX = IX,
      IY = IY,
      I = _i,
      R = _r,
      PC = _pc,
      SP = _sp,
      AFs = _afShadow,
      BCs = _bcShadow,
      DEs = _deShadow,
      HLs = _hlShadow,
      Halt = _halt,
      IFF1 = _iff1,
      IFF2 = _iff2
    };
    _bus.SaveState(state);
    return state;
  }

  public readonly void SaveState(Snapshot state)
  {
    state.AF = AF;
    state.BC = BC;
    state.DE = DE;
    state.HL = HL;
    state.IX = IX;
    state.IY = IY;
    state.I = _i;
    state.R = _r;
    state.PC = _pc;
    state.SP = _sp;
    state.AFs = _afShadow;
    state.BCs = _bcShadow;
    state.DEs = _deShadow;
    state.HLs = _hlShadow;
    state.Halt = _halt;
    state.IFF1 = _iff1;
    state.IFF2 = _iff2;
    _bus.SaveState(state);
  }

  public readonly Snapshot ReadState()
  {
    var state = new Snapshot
    {
      AF = AF,
      BC = BC,
      DE = DE,
      HL = HL,
      IX = IX,
      IY = IY,
      I = _i,
      R = _r,
      PC = _pc,
      SP = _sp,
      AFs = _afShadow,
      BCs = _bcShadow,
      DEs = _deShadow,
      HLs = _hlShadow,
      Halt = _halt,
      IFF1 = _iff1,
      IFF2 = _iff2
    };
    _bus.SaveState(state);
    return state;
  }

  public readonly string DumpRegisters()
  {
    return "╒══════════╤══════════╤══════════╤══════════╤═══════════╕\r\n" +
           $"│ PC: {_pc.ToHex()} │ SP: {_sp.ToHex()} │ IX: {IX.ToHex()} │ IY: {IY.ToHex()} │ R: {_r.ToHex()}     │\r\n" +
           $"│ AF: {AF.ToHex()} │ BC: {BC.ToHex()} │ DE: {DE.ToHex()} │ HL: {HL.ToHex()} │ IFF1: {_iff1.ToBit()}   │\r\n" +
           $"│     {_afShadow.ToHex()} │     {_bcShadow.ToHex()} │     {_deShadow.ToHex()} │     {_hlShadow.ToHex()} │ IFF2: {_iff2.ToBit()}   │\r\n" +
           "╘══════════╧══════════╧══════════╧══════════╧═══════════╛";
  }

  public override readonly string ToString() => DumpRegisters() + "\r\n" +
                                                $"Flags: {_flags} | CIR: {_instruction}";
  #endregion
}
