using Quill.Definitions;
using Quill.Extensions;
using static Quill.Z80.Opcodes;

namespace Quill.Z80
{
  public partial class CPU
  {
    private Memory _mem;
    private Registers _reg;
    private int _cycleCount;
    private int _instructionCount;

    public CPU()
    {
      _mem = new Memory();
      _reg = new Registers();
    }
    
    public void Step()
    {
      FetchInstruction();
      ExecuteInstruction();

      _instructionCount++;
    }
    
    private Instruction _cir = new Opcodes.Instruction();
    private ushort _address = 0x00;

    private byte FetchByte() => _mem[_reg.PC++];

    private ushort FetchWord()
    {
      var lowByte = FetchByte();
      var highByte = FetchByte();
      return highByte.Concatenate(lowByte);
    }

    private void FetchInstruction()
    {
      var opcode = new byte[3];
      opcode[0] = FetchByte();

      if (!Opcodes.IsPrefix(opcode[0]))
      {
        _cir = Opcodes.Decode(opcode);
        return;
      }

      opcode[1] = FetchByte();
      
      if (opcode[1] == 0xCB &&
         (opcode[0] == 0xDD || opcode[1] == 0xFD))
      {
        opcode[2] = FetchByte();
      }
 
        _cir = Opcodes.Decode(opcode);
    }

    private void ExecuteInstruction()
    {
      switch (_cir.Operation)
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
    }
    
    private byte GetByteOperand(Operand operand)
    {
      switch (operand)
      {
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
          return _reg.ReadByte(operand);

        case Operand.Indirect:
          _address = FetchWord();
          break;

        case Operand.BCi:
        case Operand.DEi:
        case Operand.HLi:
          _address = _reg.ReadWord(operand);
          break;

        case Operand.IXd:
        case Operand.IYd:
          _address = (byte)(_reg.ReadByte(operand) + FetchByte());
          break;
        
        default:
          return _reg.A;
      }
      return _mem[_address];
    }

    private ushort GetWordOperand(Operand operand)
    {
      switch (operand)
      {
        case Operand.Immediate:
          return FetchWord();

        case Operand.Indirect:
          _address = FetchWord();
          break;

        case Operand.AF:
        case Operand.BC: 
        case Operand.DE: 
        case Operand.HL: 
        case Operand.IX:  
        case Operand.IY: 
        case Operand.PC:
        case Operand.SP:
          return _reg.ReadWord(operand);

        default:
          return 0x00;
      }
      return _mem[_address];
    }

    private void SetByteValue(byte value)
    {
      switch (_cir.Destination)
      {
        case Operand.A:
        case Operand.B:
        case Operand.C:
        case Operand.D:
        case Operand.E:
        case Operand.F:
        case Operand.H:
        case Operand.L:
          _reg.WriteByte(_cir.Destination, value);
          return;

        case Operand.Indirect:
          _address = FetchWord();
          break;
        
        case Operand.BCi:
          _address = _reg.BC;
          break;

        case Operand.DEi:
          _address = _reg.DE;
          break;

        case Operand.HLi:
          _address = _reg.HL;
          break;

        case Operand.IXd:
        case Operand.IYd:
          _address = (byte)(_reg.ReadByte(_cir.Source) + FetchByte());
          break;
      }
      _mem[_address] = value;
    }

    private void SetWordValue(ushort value)
    {
      switch (_cir.Destination)
      {
        case Operand.Indirect:
          _mem.WriteWord(_address, value);
          return;

        case Operand.AF:
        case Operand.BC:
        case Operand.DE:
        case Operand.HL:
        case Operand.IX:
        case Operand.IY:
        case Operand.PC:
        case Operand.SP:
          _reg.WriteWord(_cir.Destination, value);
          return;
      }
    }

    private void SetFlagsForByte(int result)
    {
        _reg.Sign = ((result >> 7) & 1) != 0;
        _reg.Zero = (result == 0);
        _reg.Carry = (result > byte.MaxValue);
    }
    
    private void SetFlagsForWord(int result)
    {
        _reg.Sign = ((result >> 15) & 1) != 0;
        _reg.Zero = (result == 0);
        _reg.Carry = (result > ushort.MaxValue);
    }

    public override String ToString() => _reg.ToString() + $"\r\nInstruction Count: {_instructionCount} "; 
  }
}