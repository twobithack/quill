using Quill.Definitions;
using Quill.Extensions;

namespace Quill.Z80
{
  public class Registers
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
      get => Util.ConcatBytes(A, F);
      set => value.ExtractBytes(ref A, ref F);
    }

    public ushort BC
    {
      get => Util.ConcatBytes(B, C);
      set => value.ExtractBytes(ref B, ref C);
    }

    public ushort DE
    {
      get => Util.ConcatBytes(D, E);
      set => value.ExtractBytes(ref D, ref E);
    }

    public ushort HL
    {
      get => Util.ConcatBytes(H, L);
      set => value.ExtractBytes(ref H, ref L);
    }

    public ushort AFp
    {
      get => Util.ConcatBytes(Ap, Fp);
      set => value.ExtractBytes(ref Ap, ref Fp);
    }

    public ushort BCp
    {
      get => Util.ConcatBytes(Bp, Cp);
      set => value.ExtractBytes(ref Bp, ref Cp);
    }

    public ushort DEp
    {
      get => Util.ConcatBytes(Dp, Ep);
      set => value.ExtractBytes(ref Dp, ref Ep);
    }

    public ushort HLp
    {
      get => Util.ConcatBytes(Hp, Lp);
      set => value.ExtractBytes(ref Hp, ref Lp);
    }
    
    public Flags Flags
    {
      get => (Flags) F;
      set => F = (byte) value;
    }

    public void SetFlag(Flags flag, bool value) => Flags = value
                                                          ? Flags | flag 
                                                          : Flags & ~flag;
    
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
        default:        return 0x00;
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
        default:         return 0x00;
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

    public override String ToString()
    {
      return  $"╒══════════╤════════════╤════════════╤════════════╤════════════╕\r\n" +
              $"│Registers │  AF: {AF.ToHex()} │  BC: {BC.ToHex()} │  DE: {DE.ToHex()} │  HL: {HL.ToHex()} │\r\n" +
              $"│          │  IX: {IX.ToHex()} │  IY: {IY.ToHex()} │  PC: {PC.ToHex()} │  SP: {SP.ToHex()} │\r\n" +
              $"╘══════════╧════════════╧════════════╧════════════╧════════════╛\r\n" +
              $"Flags: {Flags.ToString()}";
    }
  }
}