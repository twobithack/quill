namespace Sonic.Definitions
{
  public static class InstructionSet
  {
    public struct Instruction
    {
      public OP[] Operation;
      public Operand Source;
      public Operand Destination;
    }

    public static readonly byte[] Prefixes = new byte[] { 0xCB, 0xDD, 0xED ,0xFD };

    public static OP GetInstruction(byte p0, byte p1, byte op) => GetOpcodeTable(p0, p1)[op];

    public static OP[] GetOpcodeTable(byte prefix0, byte prefix1) => new { prefix0, prefix1 } switch
    {
      { prefix0: 0x00 }                 => _mainOps,
      { prefix0: 0xCB }                 => _bitOps,
      { prefix0: 0xDD, prefix1: 0xCB }  => _ixBitOps,
      { prefix0: 0xDD }                 => _ixOps,
      { prefix0: 0xED }                 => _miscOps,
      { prefix0: 0xFD, prefix1: 0xCB }  => _iyBitOps,
      { prefix0: 0xFD }                 => _iyOps,
      _ => throw new InvalidOperationException()
    };

    private static readonly OP[] _mainOps = new OP[]
    { // 0         1         2         3         4         5         6         7         8         9         A         B         C         D         E         F
      OP.NOP,   OP.LD,    OP.LD,    OP.INC,   OP.INC,   OP.DEC,   OP.LD,    OP.RLCA,  OP.EX,    OP.ADD,   OP.LD,    OP.DEC,   OP.INC,   OP.DEC,   OP.LD,    OP.RRCA,  
      OP.DJNZ,  OP.LD,    OP.LD,    OP.INC,   OP.INC,   OP.DEC,   OP.LD,    OP.RLA,   OP.JR,    OP.ADD,   OP.LD,    OP.DEC,   OP.INC,   OP.DEC,   OP.LD,    OP.RRA, 
      OP.JR,    OP.LD,    OP.LD,    OP.INC,   OP.INC,   OP.DEC,   OP.LD,    OP.DAA,   OP.JR,    OP.ADD,   OP.LD,    OP.DEC,   OP.INC,   OP.DEC,   OP.LD,    OP.CPL,
      OP.JR,    OP.LD,    OP.LD,    OP.INC,   OP.INC,   OP.DEC,   OP.LD,    OP.SCF,   OP.JR,    OP.ADD,   OP.LD,    OP.DEC,   OP.INC,   OP.DEC,   OP.LD,    OP.CCF,      
      OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,
      OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,
      OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,
      OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.HALT,  OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,
      OP.ADD,   OP.ADD,   OP.ADD,   OP.ADD,   OP.ADD,   OP.ADD,   OP.ADD,   OP.ADD,   OP.ADC,   OP.ADC,   OP.ADC,   OP.ADC,   OP.ADC,   OP.ADC,   OP.ADC,   OP.ADC,
      OP.SUB,   OP.SUB,   OP.SUB,   OP.SUB,   OP.SUB,   OP.SUB,   OP.SUB,   OP.SUB,   OP.SBC,   OP.SBC,   OP.SBC,   OP.SBC,   OP.SBC,   OP.SBC,   OP.SBC,   OP.SBC,
      OP.AND,   OP.AND,   OP.AND,   OP.AND,   OP.AND,   OP.AND,   OP.AND,   OP.AND,   OP.XOR,   OP.XOR,   OP.XOR,   OP.XOR,   OP.XOR,   OP.XOR,   OP.XOR,   OP.XOR, 
      OP.OR,    OP.OR,    OP.OR,    OP.OR,    OP.OR,    OP.OR,    OP.OR,    OP.OR,    OP.CP,    OP.CP,    OP.CP,    OP.CP,    OP.CP,    OP.CP,    OP.CP,    OP.CP,  
      OP.RET,   OP.POP,   OP.JP,    OP.JP,    OP.CALL,  OP.PUSH,  OP.ADD,   OP.RST,   OP.RET,   OP.RET,   OP.JP,    OP.NOP,   OP.CALL,  OP.CALL,  OP.ADC,   OP.RST,
      OP.RET,   OP.POP,   OP.JP,    OP.OUT,   OP.CALL,  OP.PUSH,  OP.SUB,   OP.RST,   OP.RET,   OP.EXX,   OP.JP,    OP.IN,    OP.CALL,  OP.NOP,   OP.SBC,   OP.RST,
      OP.RET,   OP.POP,   OP.JP,    OP.EX,    OP.CALL,  OP.PUSH,  OP.AND,   OP.RST,   OP.RET,   OP.JP,    OP.JP,    OP.EX,    OP.CALL,  OP.NOP,   OP.XOR,   OP.RST,
      OP.RET,   OP.POP,   OP.JP,    OP.DI,    OP.CALL,  OP.PUSH,  OP.OR,    OP.RST,   OP.RET,   OP.LD,    OP.JP,    OP.EI,    OP.CALL,  OP.NOP,   OP.CP,    OP.RST
    };

    private static readonly OP[] _bitOps =
    { // 0         1         2         3         4         5         6         7         8         9         A         B         C         D         E         F
      OP.RLC,   OP.RLC,   OP.RLC,   OP.RLC,   OP.RLC,   OP.RLC,   OP.RLC,   OP.RLC,   OP.RRC,   OP.RRC,   OP.RRC,   OP.RRC,   OP.RRC,   OP.RRC,   OP.RRC,   OP.RRC,
      OP.RL,    OP.RL,    OP.RL,    OP.RL,    OP.RL,    OP.RL,    OP.RL,    OP.RL,    OP.RR,    OP.RR,    OP.RR,    OP.RR,    OP.RR,    OP.RR,    OP.RR,    OP.RR,
      OP.SLA,   OP.SLA,   OP.SLA,   OP.SLA,   OP.SLA,   OP.SLA,   OP.SLA,   OP.SLA,   OP.SRA,   OP.SRA,   OP.SRA,   OP.SRA,   OP.SRA,   OP.SRA,   OP.SRA,   OP.SRA,
      OP.SLL,   OP.SLL,   OP.SLL,   OP.SLL,   OP.SLL,   OP.SLL,   OP.SLL,   OP.SLL,   OP.SRL,   OP.SRL,   OP.SRL,   OP.SRL,   OP.SRL,   OP.SRL,   OP.SRL,   OP.SRL,
      OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT, 
      OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT, 
      OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT, 
      OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT, 
      OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,
      OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,
      OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,
      OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,
      OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,
      OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,
      OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,
      OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET
    };

    private static readonly OP[] _ixOps =
    { // 0         1         2         3         4         5         6         7         8         9         A         B         C         D         E         F
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.ADD,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP, 
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.ADD,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,
      OP.NOP,   OP.LD,    OP.LD,    OP.INC,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.ADD,   OP.LD,    OP.DEC,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.INC,   OP.DEC,   OP.LD,    OP.NOP,   OP.NOP,   OP.ADD,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.LD,    OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.LD,    OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.LD,    OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.LD,    OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.LD,    OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.LD,    OP.NOP,
      OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.NOP,   OP.LD,    OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.LD,    OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.ADD,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.ADC,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.SUB,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.SBC,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.AND,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.XOR,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.OR,    OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.CP,    OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,
      OP.NOP,   OP.POP,   OP.NOP,   OP.EX,    OP.NOP,   OP.PUSH,  OP.NOP,   OP.NOP,   OP.NOP,   OP.JP,    OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.LD,    OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP
    };

    private static readonly OP[] _ixBitOps =
    { // 0         1         2         3         4         5         6         7         8         9         A         B         C         D         E         F
      OP.RLC,   OP.RLC,   OP.RLC,   OP.RLC,   OP.RLC,   OP.RLC,   OP.RLC,   OP.RLC,   OP.RRC,   OP.RRC,   OP.RRC,   OP.RRC,   OP.RRC,   OP.RRC,   OP.RRC,   OP.RRC,
      OP.RL,    OP.RL,    OP.RL,    OP.RL,    OP.RL,    OP.RL,    OP.RL,    OP.RL,    OP.RR,    OP.RR,    OP.RR,    OP.RR,    OP.RR,    OP.RR,    OP.RR,    OP.RR,
      OP.SLA,   OP.SLA,   OP.SLA,   OP.SLA,   OP.SLA,   OP.SLA,   OP.SLA,   OP.SLA,   OP.SRA,   OP.SRA,   OP.SRA,   OP.SRA,   OP.SRA,   OP.SRA,   OP.SRA,   OP.SRA,
      OP.SLL,   OP.SLL,   OP.SLL,   OP.SLL,   OP.SLL,   OP.SLL,   OP.SLL,   OP.SLL,   OP.SRL,   OP.SRL,   OP.SRL,   OP.SRL,   OP.SRL,   OP.SRL,   OP.SRL,   OP.SRL,
      OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT, 
      OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT, 
      OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT, 
      OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT,   OP.BIT, 
      OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,
      OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,
      OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,
      OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,   OP.RES,
      OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,
      OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,
      OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,
      OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET,   OP.SET
    };
    
    private static readonly OP[] _miscOps = 
    { // 0         1         2         3         4         5         6         7         8         9         A         B         C         D         E         F
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,
      OP.IN,    OP.OUT,   OP.SBC,   OP.LD,    OP.NEG,   OP.RETN,  OP.IM,    OP.LD,    OP.IN,    OP.OUT,   OP.ADC,   OP.LD,    OP.NOP,   OP.RETI,  OP.NOP,   OP.LD,
      OP.IN,    OP.NOP,   OP.SBC,   OP.LD,    OP.NOP,   OP.NOP,   OP.IM,    OP.LD,    OP.IN,    OP.OUT,   OP.ADC,   OP.LD,    OP.NOP,   OP.NOP,   OP.IM,    OP.LD,
      OP.IN,    OP.NOP,   OP.SBC,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.RRD,   OP.IN,    OP.OUT,   OP.ADC,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.RLD,
      OP.NOP,   OP.NOP,   OP.SBC,   OP.LD,    OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.IN,    OP.OUT,   OP.ADC,   OP.LD,    OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,
      OP.LDI,   OP.CPI,   OP.INI,   OP.OUTI,  OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.LDD,   OP.CPD,   OP.IND,   OP.OUTD,  OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,
      OP.LDIR,  OP.CPIR,  OP.INIR,  OP.OTIR,  OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.LDDR,  OP.CPDR,  OP.INDR,  OP.OTDR,  OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,
    };
    
    private static readonly OP[] _iyOps = 
    { // 0         1         2         3         4         5         6         7         8         9         A         B         C         D         E         F
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.ADD,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.ADD,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,
      OP.NOP,   OP.LD,    OP.LD,    OP.INC,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.ADD,   OP.LD,    OP.DEC,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.INC,   OP.DEC,   OP.LD,    OP.NOP,   OP.NOP,   OP.ADD,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.LD,    OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.LD,    OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.LD,    OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.LD,    OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.LD,    OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.LD,    OP.NOP,
      OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.LD,    OP.NOP,   OP.LD,    OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.LD,    OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.ADD,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.ADC,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.SUB,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.SBC,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.AND,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.XOR,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.OR,    OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.CP,    OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,
      OP.NOP,   OP.POP,   OP.NOP,   OP.EX,    OP.NOP,   OP.PUSH,  OP.NOP,   OP.NOP,   OP.NOP,   OP.JP,    OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.LD,    OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP
    };
    
    private static readonly OP[] _iyBitOps = 
    { // 0         1         2         3         4         5         6         7         8         9         A         B         C         D         E         F
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.RLC,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.RRC,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.RL,    OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.RR,    OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.SLA,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.SRA,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.SRL,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.BIT,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.BIT,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.BIT,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.BIT,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.BIT,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.BIT,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.BIT,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.BIT,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.RES,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.RES,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.RES,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.RES,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.RES,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.RES,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.RES,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.RES,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.SET,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.SET,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.SET,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.SET,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.SET,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.SET,   OP.NOP,
      OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.SET,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.NOP,   OP.SET,   OP.NOP
    };
  }
}