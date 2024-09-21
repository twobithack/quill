using Quill.Extensions;
using static Quill.Z80.Opcodes;

namespace Quill.Z80
{
  public partial class CPU
  {
    private ushort PC;
    private ushort SP;
    private ushort IX;
    private ushort IY;
    private byte A;
    private byte B;
    private byte C;
    private byte D;
    private byte E;
    private byte F;
    private byte H;
    private byte L;
    private byte I;
    private byte R;
    private byte Ap;
    private byte Bp;
    private byte Cp;
    private byte Dp;
    private byte Ep;
    private byte Fp;
    private byte Hp;
    private byte Lp;
    private bool Iff1;
    private bool Iff2;
    private Memory _mem;
    private Opcode _cir;
    private ushort _address;
    private int _cycleCount;
    private int _instructionCount;

    public CPU()
    {
      _mem = new Memory();
      _cir = new Opcode();
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

    private Flags _flags
    {
      get => (Flags) F;
      set => F = (byte) value;
    }

    private void SetFlag(Flags flag, bool value) => _flags = value
                                                          ? _flags | flag 
                                                          : _flags & ~flag;

    public void LoadProgram(byte[] rom)
    {
      for (ushort i = 0x00; i < rom.Count(); i++) 
        _mem[i] = rom[i];
    }
    
    public void Step()
    {
      FetchInstruction();
      ExecuteInstruction();

      _instructionCount++;
    }
    
    private byte FetchByte() => _mem[PC++];

    private ushort FetchWord()
    {
      var lowByte = FetchByte();
      var highByte = FetchByte();
      return highByte.Append(lowByte);
    }

    private void FetchInstruction()
    {
      var op = FetchByte();

      switch (op)
      {
        case 0xCB:  DecodeCBInstruction();  return;
        case 0xDD:  DecodeDDInstruction();  return;
        case 0xED:  DecodeEDInstruction();  return;
        case 0xFD:  DecodeFDInstruction();  return;
        default:    DecodeInstruction(op);  return;
      }
    }

    private void DecodeInstruction(byte op) => _cir = Opcodes.Main[op];

    private void DecodeCBInstruction()
    {
      var op = FetchByte();
      _cir = Opcodes.Bit[op];
    }

    private void DecodeDDInstruction()
    {
      var op = FetchByte();

      if (op != 0xCB)
      {
        _cir = Opcodes.IX[op];
        return;
      }
        
      op = FetchByte();
      _cir = Opcodes.BitIX[op];
    }

    private void DecodeEDInstruction()
    {
      var op = FetchByte();
      _cir = Opcodes.Misc[op];
    }

    private void DecodeFDInstruction()
    {
      var op = FetchByte();

      if (op != 0xCB)
      {
        _cir = Opcodes.IY[op];
        return;
      }
      
      op = FetchByte();
      _cir = Opcodes.BitIY[op];
    }

    private byte ReadByte(Operand operand)
    {
      switch (operand)
      {
        case Operand.Indirect:
          _address = FetchWord();
          break;

        case Operand.BCi:
        case Operand.DEi:
        case Operand.HLi:
          _address = ReadRegisterPair(operand);
          break;

        case Operand.IXd:
        case Operand.IYd:
          _address = (byte)(ReadRegister(operand) + FetchByte());
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
          return ReadRegister(operand);
        
        default: throw new InvalidOperationException();
      }
      return _mem[_address];
    }

    private ushort ReadWord(Operand operand)
    {
      switch (operand)
      {
        case Operand.Indirect:
          _address = FetchWord();
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
          return ReadRegisterPair(operand);

        default:
          return 0x00;
      }
      return _mem[_address];
    }

    private void WriteByte(byte value)
    {
      switch (_cir.Destination)
      {
        case Operand.A: A = value; return;
        case Operand.B: B = value; return;
        case Operand.C: C = value; return;
        case Operand.D: D = value; return;
        case Operand.E: E = value; return;
        case Operand.F: F = value; return;
        case Operand.H: H = value; return;
        case Operand.L: L = value; return;

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
          _address = (byte)(ReadRegister(_cir.Source) + FetchByte());
          break;
      }
      _mem[_address] = value;
    }

    private void WriteWord(ushort value)
    {
      switch (_cir.Destination)
      {
        case Operand.Indirect:
          _mem.WriteWord(_address, value);
          return;

        case Operand.AF: AF = value; return;
        case Operand.BC: BC = value; return;
        case Operand.DE: DE = value; return;
        case Operand.HL: HL = value; return;
        case Operand.IX: IX = value; return;
        case Operand.IY: IY = value; return;
        case Operand.PC: PC = value; return;
        case Operand.SP: SP = value; return;
        
        default: throw new InvalidOperationException();
      }
    }

    private byte ReadRegister(Operand register)
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

    private ushort ReadRegisterPair(Operand register)
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

    private void SetArithmeticFlags(int result)
    {
        Sign = ((result >> 7) & 1) != 0;
        Zero = (result == 0);
        Carry = (result > byte.MaxValue);
    }
    
    private void SetArithmeticFlags16(int result)
    {
        Sign = ((result >> 15) & 1) != 0;
        Zero = (result == 0);
        Carry = (result > ushort.MaxValue);
    }

    private bool IsWordOperation() => _wordOperands.Contains(_cir.Destination) ||
                                      _wordOperands.Contains(_cir.Source);

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

    public override String ToString()
    {
      return  $"╒══════════╤═══════════╤═══════════╤═══════════╤═══════════╕\r\n" +
              $"│Registers │ AF: {AF.ToHex()} │ BC: {BC.ToHex()} │ DE: {DE.ToHex()} │ HL: {HL.ToHex()} │\r\n" +
              $"│          │ IX: {IX.ToHex()} │ IY: {IY.ToHex()} │ PC: {PC.ToHex()} │ SP: {SP.ToHex()} │\r\n" +
              $"╘══════════╧═══════════╧═══════════╧═══════════╧═══════════╛\r\n" +
              $"Flags: {_flags.ToString()}\r\nInstruction Count: {_instructionCount} "; 
    }
  }
}