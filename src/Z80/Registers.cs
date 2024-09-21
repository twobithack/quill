using Quill.Extensions;

namespace Quill.Z80
{
  public partial class CPU
  {
    // special-purpose registers
    public ushort PC;
    public ushort SP;
    public ushort IX;
    public ushort IY;
    public byte I;
    public byte R;
    public byte A;
    public byte F;

    // general-purpose registers
    public byte B;
    public byte C;
    public byte D;
    public byte E;
    public byte H;
    public byte L;

    // complimentary registers
    public byte Ap;
    public byte Bp;
    public byte Cp;
    public byte Dp;
    public byte Ep;
    public byte Fp;
    public byte Hp;
    public byte Lp;

    // interrupt flags
    public bool Iff1;
    public bool Iff2;

    // register pair mappings
    public ushort AF
    {
      get => A.Concatenate(F);
      set
      {
        A = value.GetHighByte();
        F = value.GetLowByte();
      }
    }

    public ushort BC
    {
      get => B.Concatenate(C);
      set
      {
        B = value.GetHighByte();
        C = value.GetLowByte();
      }
    }

    public ushort DE
    {
      get => D.Concatenate(E);
      set
      {
        D = value.GetHighByte();
        E = value.GetLowByte();
      }
    }

    public ushort HL
    {
      get => H.Concatenate(L);
      set
      {
        H = value.GetHighByte();
        L = value.GetLowByte();
      }
    }

    public bool Sign
    {
      get => _flags.HasFlag(Flags.Sign);
      set => SetFlag(Flags.Sign, value);
    }

    public bool Zero
    {
      get => _flags.HasFlag(Flags.Zero);
      set => SetFlag(Flags.Zero, value);
    }

    public bool Halfcarry
    {
      get => _flags.HasFlag(Flags.Halfcarry);
      set => SetFlag(Flags.Halfcarry, value);
    }

    public bool Parity
    {
      get => _flags.HasFlag(Flags.Parity);
      set => SetFlag(Flags.Parity, value);
    }

    public bool Overflow
    {
      get => _flags.HasFlag(Flags.Parity);
      set => SetFlag(Flags.Parity, value);
    }

    public bool Negative
    {
      get => _flags.HasFlag(Flags.Negative);
      set => SetFlag(Flags.Negative, value);
    }

    public bool Carry
    {
      get => _flags.HasFlag(Flags.Carry);
      set => SetFlag(Flags.Carry, value);
    }
    
    public byte ReadByte(Operand register)
    {
      switch (register)
      {
        case Operand.A: return A;
        case Operand.B: return B;
        case Operand.C: return C;
        case Operand.D: return D;
        case Operand.E: return E;
        case Operand.F: return F;
        case Operand.H: return H;
        case Operand.L: return L;
        default: throw new InvalidOperationException();
      }
    }

    public ushort ReadWord(Operand register)
    {
      switch (register)
      {
        case Operand.AF: return AF;
        case Operand.BC: return BC;
        case Operand.DE: return DE;
        case Operand.HL: return HL;
        case Operand.IX: return IX;
        case Operand.IY: return IY;
        case Operand.PC: return PC;
        case Operand.SP: return SP;
        default: throw new InvalidOperationException();
      }
    }

    public void WriteByte(Operand register, byte value)
    {
      switch (register)
      {
        case Operand.A: A = value; return;
        case Operand.B: B = value; return;
        case Operand.C: C = value; return;
        case Operand.D: D = value; return;
        case Operand.E: E = value; return;
        case Operand.F: F = value; return;
        case Operand.H: H = value; return;
        case Operand.L: L = value; return;
      }
    }

    public void WriteWord(Operand register, ushort value)
    {
      switch (register)
      {
        case Operand.AF: AF = value; return;
        case Operand.BC: BC = value; return;
        case Operand.DE: DE = value; return;
        case Operand.HL: HL = value; return;
        case Operand.IX: IX = value; return;
        case Operand.IY: IY = value; return;
        case Operand.PC: PC = value; return;
        case Operand.SP: SP = value; return;
      }
    }

    private Flags _flags
    {
      get => (Flags) F;
      set => F = (byte) value;
    }

    private void SetFlag(Flags flag, bool value) => _flags = value
                                                          ? _flags | flag 
                                                          : _flags & ~flag;
  }
}