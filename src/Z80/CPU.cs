using Quill.Extensions;
using static Quill.Z80.Opcodes;

namespace Quill.Z80
{
  public partial class CPU
  {
    private Registers _registers;
    private Memory _memory;
    private ushort _memPtr;
    private int _cycleCount;
    private int _instructionCount;

    public CPU()
    {
      _memory = new Memory();
      _registers = new Registers();
    }

    public void LoadProgram(byte[] rom)
    {
      for (ushort i = 0x00; i < rom.Count(); i++) 
        _memory[i] = rom[i];
    }

    private byte FetchByte() => _memory[_registers.PC++];

    private ushort FetchWord()
    {
      var lowByte = FetchByte();
      var highByte = FetchByte();
      return highByte.Append(lowByte);
    }
    
    public void Step()
    {
      var op = FetchByte();
      _registers.Instruction = op switch
      {
        0xCB  =>  Opcodes.CB[FetchByte()],
        0xDD  =>  DecodeDDInstruction(),
        0xED  =>  Opcodes.ED[FetchByte()],
        0xFD  =>  DecodeFDInstruction(),
        _     =>  Opcodes.Main[op]
      };

      switch (_registers.Instruction.Operation)
      {
        case Operation.ADC:   ADC();  break;
        case Operation.ADD:   ADD();  break;
        case Operation.AND:   AND();  break;
        case Operation.BIT:   BIT();  break;
        case Operation.CALL:  CALL(); break;
        case Operation.CCF:   CCF();  break;
        case Operation.CP:    CP();   break;
        case Operation.CPD:   CPD();  break;
        case Operation.CPI:   CPI();  break;
        case Operation.CPIR:  CPIR(); break;
        case Operation.CPL:   CPL();  break;
        case Operation.DAA:   DAA();  break;
        case Operation.DEC:   DEC();  break;
        case Operation.DI:    DI();   break;
        case Operation.DJNZ:  DJNZ(); break;
        case Operation.EI:    EI();   break;
        case Operation.EX:    EX();   break;
        case Operation.EXX:   EXX();  break;
        case Operation.HALT:  HALT(); break;
        case Operation.IM:    IM();   break;
        case Operation.IN:    IN();   break;
        case Operation.INC:   INC();  break;
        case Operation.IND:   IND();  break;
        case Operation.INI:   INI();  break;
        case Operation.INIR:  INIR(); break;
        case Operation.JP:    JP();   break;
        case Operation.JR:    JR();   break;
        case Operation.LD:    LD();   break;
        case Operation.NEG:   NEG();  break;
        case Operation.NOP:   NOP();  break;
        case Operation.OR:    OR();   break;
        case Operation.OTDR:  OTDR(); break;
        case Operation.OTIR:  OTIR(); break;
        case Operation.OUT:   OUT();  break;
        case Operation.OUTD:  OUTD(); break;
        case Operation.OUTI:  OUTI(); break;
        case Operation.POP:   POP();  break;
        case Operation.PUSH:  PUSH(); break;
        case Operation.RES:   RES();  break;
        case Operation.RL:    RL();   break;
        case Operation.RLA:   RLA();  break;
        case Operation.RLC:   RLC();  break;
        case Operation.RLCA:  RLCA(); break;
        case Operation.RLD:   RLD();  break;
        case Operation.RR:    RR();   break;
        case Operation.RRA:   RRA();  break;
        case Operation.RRC:   RRC();  break;
        case Operation.RRCA:  RRCA(); break;
        case Operation.RRD:   RRD();  break;
        case Operation.RST:   RST();  break;
        case Operation.SBC:   SBC();  break;
        case Operation.SCF:   SCF();  break;
        case Operation.SET:   SET();  break;
        case Operation.SLA:   SLA();  break;
        case Operation.SRA:   SRA();  break;
        case Operation.SRL:   SRL();  break;
        case Operation.SUB:   SUB();  break;
        case Operation.XOR:   XOR();  break;
      }

      _instructionCount++;
    }

    private Opcode DecodeDDInstruction()
    {
      var op = FetchByte();

      if (op != 0xCB)
      {
        return Opcodes.DD[op];
      }
        
      op = FetchByte();
      return Opcodes.DDCB[op];
    }
    
    private Opcode DecodeFDInstruction()
    {
      var op = FetchByte();

      if (op != 0xCB)
      {
        return Opcodes.FD[op];
      }
      
      op = FetchByte();
      return Opcodes.FDCB[op];
    }

    private byte ReadByte(Operand operand)
    {
      switch (operand)
      {
        case Operand.Indirect:
          _memPtr = FetchWord();
          break;

        case Operand.BCi:
        case Operand.DEi:
        case Operand.HLi:
          _memPtr = _registers.ReadPair(operand);
          break;

        case Operand.IXd:
        case Operand.IYd:
          _memPtr = (byte)(_registers.Read(operand) + FetchByte());
          break;

        case Operand.Immediate:
          return FetchByte();

        case Operand.A:
        case Operand.B:
        case Operand.C:
        case Operand.D:
        case Operand.E:
        case Operand.F:
        case Operand.H:
        case Operand.L:
          return _registers.Read(operand);
        
        default: throw new InvalidOperationException();
      }
      return _memory[_memPtr];
    }

    private ushort ReadWord(Operand operand)
    {
      switch (operand)
      {
        case Operand.Indirect:
          _memPtr = FetchWord();
          break;

        case Operand.Immediate:
          return FetchWord();

        case Operand.AF:
        case Operand.BC: 
        case Operand.DE: 
        case Operand.HL: 
        case Operand.IX:  
        case Operand.IY: 
        case Operand.PC:
        case Operand.SP:
          return _registers.ReadPair(operand);

        default:
          return 0x00;
      }
      return _memory[_memPtr];
    }

    private void WriteByte(byte value)
    {
      switch (_registers.Instruction.Destination)
      {
        case Operand.A: _registers.A = value; return;
        case Operand.B: _registers.B = value; return;
        case Operand.C: _registers.C = value; return;
        case Operand.D: _registers.D = value; return;
        case Operand.E: _registers.E = value; return;
        case Operand.F: _registers.F = value; return;
        case Operand.H: _registers.H = value; return;
        case Operand.L: _registers.L = value; return;

        case Operand.Indirect:  
          _memPtr = FetchWord();
          break;

        case Operand.IXd:
        case Operand.IYd:
          _memPtr = (byte)(_registers.Read(_registers.Instruction.Source) + FetchByte());
          break;

        case Operand.BCi: _memPtr = _registers.BC; break;
        case Operand.DEi: _memPtr = _registers.DE; break;
        case Operand.HLi: _memPtr = _registers.HL; break;

        default: throw new InvalidOperationException();
      }
      _memory[_memPtr] = value;
    }

    private void WriteWord(ushort value)
    {
      switch (_registers.Instruction.Destination)
      {
        case Operand.Indirect:
          _memory.WriteWord(_memPtr, value);
          return;

        case Operand.AF: _registers.AF = value; return;
        case Operand.BC: _registers.BC = value; return;
        case Operand.DE: _registers.DE = value; return;
        case Operand.HL: _registers.HL = value; return;
        case Operand.IX: _registers.IX = value; return;
        case Operand.IY: _registers.IY = value; return;
        case Operand.PC: _registers.PC = value; return;
        case Operand.SP: _registers.SP = value; return;

        default: throw new InvalidOperationException();
      }
    }

    private bool IsWordOperation() => _wordOperands.Contains(_registers.Instruction.Destination) ||
                                      _wordOperands.Contains(_registers.Instruction.Source);

    private static readonly Operand[] _wordOperands = new Operand[]
    {      
      Operand.AF, 
      Operand.BC, 
      Operand.DE, 
      Operand.HL, 
      Operand.IX,  
      Operand.IY, 
      Operand.PC,
      Operand.SP
    };

    public void DumpState() => _memory.DumpPage(0x00);
    public override String ToString() => _registers.ToString() + $"Instruction Count: {_instructionCount}";
  }
}