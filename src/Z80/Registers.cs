using Quill.Extensions;
using static Quill.Z80.Opcodes;

namespace Quill.Z80
{
  public class Registers
  {
    public ushort PC;
    public ushort SP;
    public ushort IX;
    public ushort IY;

    public byte A;
    public byte B;
    public byte C;
    public byte D;
    public byte E;
    public byte F;
    public byte H;
    public byte L;
    public byte I;
    public byte R;
    public byte Ap;
    public byte Bp;
    public byte Cp;
    public byte Dp;
    public byte Ep;
    public byte Fp;
    public byte Hp;
    public byte Lp;

    public bool IFF1;
    public bool IFF2;

    public Opcode Instruction;

    public Registers()
    {
      Instruction = new Opcode();
    }

    public void Reset()
    { 
      A = 0;
      B = 0;
      C = 0;
      D = 0;
      E = 0;
      F = 0;
      H = 0;
      L = 0;
      I = 0;
      R = 0;
      Ap = 0;
      Bp = 0;
      Cp = 0;
      Dp = 0;
      Ep = 0;
      Fp = 0;
      Hp = 0;
      Lp = 0;
      PC = 0;
      SP = 0;
      IX = 0;
      IY = 0;
      IFF1 = false;
      IFF2 = false;
      Instruction = new Opcode();
    }

    public ushort AF
    {
      get => A.Append(F);
      set
      {
        A = value.GetHighByte();
        F = value.GetLowByte();
      }
    }

    public ushort BC
    {
      get => B.Append(C);
      set
      {
        B = value.GetHighByte();
        C = value.GetLowByte();
      }
    }

    public ushort DE
    {
      get => D.Append(E);
      set
      {
        D = value.GetHighByte();
        E = value.GetLowByte();
      }
    }

    public ushort HL
    {
      get => H.Append(L);
      set
      {
        H = value.GetHighByte();
        L = value.GetLowByte();
      }
    }

    public byte Read(Operand register) => register switch
    {
      Operand.A => A,
      Operand.B => B,
      Operand.C => C,
      Operand.D => D,
      Operand.E => E,
      Operand.F => F,
      Operand.H => H,
      Operand.L => L,
      _ => throw new InvalidOperationException()
    };

    public ushort ReadPair(Operand register) => register switch
    {
      Operand.AF => AF,
      Operand.BC => BC,
      Operand.DE => DE,
      Operand.HL => HL,
      Operand.IX => IX,
      Operand.IY => IY,
      Operand.PC => PC,
      Operand.SP => SP,
      _ => throw new InvalidOperationException()
    };

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

    public void SetFlag(Flags flag, bool value) => _flags = value
                                                          ? _flags | flag 
                                                          : _flags & ~flag;
    
    private Flags _flags
    {
      get => (Flags) F;
      set => F = (byte) value;
    }

    public override string ToString()
    {
      return $"╒══════════╤═══════════╤═══════════╤═══════════╤═══════════╕\r\n" +
             $"│Registers │ AF: {AF.ToHex()} │ BC: {BC.ToHex()} │ DE: {DE.ToHex()} │ HL: {HL.ToHex()} │\r\n" +
             $"│          │ IX: {IX.ToHex()} │ IY: {IY.ToHex()} │ PC: {PC.ToHex()} │ SP: {SP.ToHex()} │\r\n" +
             $"╘══════════╧═══════════╧═══════════╧═══════════╧═══════════╛\r\n" +
             $"Flags: {_flags.ToString()}\r\n";
    }
  }
}