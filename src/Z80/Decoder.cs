using static Quill.Z80.Opcodes;

namespace Quill.Z80
{
  public partial class CPU
  {
    private void FetchInstruction()
    {
      var op = FetchByte();

      switch (op)
      {
        case 0xCB:
          DecodeCBInstruction();
          return;

        case 0xDD:
          DecodeDDInstruction();
          return;

        case 0xED:
          DecodeEDInstruction();
          return;

        case 0xFD:
          DecodeFDInstruction();
          return;

        default:
          _opcode = Opcodes.Main[op];
          return;
      }
    }

    private void DecodeCBInstruction()
    {
      var op = FetchByte();
      _opcode = Opcodes.Bit[op];
    }

    private void DecodeDDInstruction()
    {
      var op = FetchByte();

      if (op != 0xCB)
      {
        _opcode = Opcodes.IX[op];
        return;
      }
        
      op = FetchByte();
      _opcode = Opcodes.BitIX[op];
    }

    private void DecodeEDInstruction()
    {
      var op = FetchByte();
      _opcode = Opcodes.Misc[op];
    }

    private void DecodeFDInstruction()
    {
      var op = FetchByte();

      if (op != 0xCB)
      {
        _opcode = Opcodes.IY[op];
        return;
      }
        
      op = FetchByte();
      _opcode = Opcodes.BitIY[op];
    }

    private void ExecuteInstruction()
    {
      switch (_opcode.Operation)
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
          return ReadByte(operand);

        case Operand.Indirect:
          _address = FetchWord();
          break;

        case Operand.BCi:
        case Operand.DEi:
        case Operand.HLi:
          _address = ReadWord(operand);
          break;

        case Operand.IXd:
        case Operand.IYd:
          _address = (byte)(ReadByte(operand) + FetchByte());
          break;
        
        default:
          return A;
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
          return ReadWord(operand);

        default:
          return 0x00;
      }
      return _mem[_address];
    }

    private void SetByteValue(byte value)
    {
      switch (_opcode.Destination)
      {
        case Operand.A:
        case Operand.B:
        case Operand.C:
        case Operand.D:
        case Operand.E:
        case Operand.F:
        case Operand.H:
        case Operand.L:
          WriteByte(_opcode.Destination, value);
          return;

        case Operand.Indirect:
          _address = FetchWord();
          break;
        
        case Operand.BCi:
          _address = BC;
          break;

        case Operand.DEi:
          _address = DE;
          break;

        case Operand.HLi:
          _address = HL;
          break;

        case Operand.IXd:
        case Operand.IYd:
          _address = (byte)(ReadByte(_opcode.Source) + FetchByte());
          break;
      }
      _mem[_address] = value;
    }

    private void SetWordValue(ushort value)
    {
      switch (_opcode.Destination)
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
          WriteWord(_opcode.Destination, value);
          return;
      }
    }

    private bool IsWordOperation() => _wordOperands.Contains(_opcode.Destination) ||
                                      _wordOperands.Contains(_opcode.Source);

    private static Operand[] _wordOperands = new Operand[]
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
  }
}