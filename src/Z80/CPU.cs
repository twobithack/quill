using Quill.Definitions;
using Quill.Extensions;
using static Quill.Definitions.Opcodes;
using System.Runtime.CompilerServices;

namespace Quill
{
  public unsafe sealed partial class CPU
  {
    private Memory _memory;
    private VDP _vdp;
    private bool _halt;
    private int _instructionCount;

    public CPU(VDP vdp)
    {
      _instruction = new Opcode();
      _memory = new Memory();
      _vdp = vdp;
    }

    public void LoadROM(byte[] rom) => _memory.LoadROM(rom);

    public void Step()
    {
      HandleInterrupts();
      DecodeInstruction();
      ExecuteInstruction();
      _instructionCount++;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HandleInterrupts()
    {
      if (_vdp.IRQ && _iff1)
      {
        _halt = false;
        _iff1 = false;
        _iff2 = false;

        _memory.WriteWord(_sp, _pc);
        _sp += 2;
        _pc = 0x38;
      }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DecodeInstruction()
    {
      var op = FetchByte();
      _instruction = op switch
      {
        0xCB  =>  DecodeCBInstruction(),
        0xDD  =>  DecodeDDInstruction(),
        0xED  =>  DecodeEDInstruction(),
        0xFD  =>  DecodeFDInstruction(),
        _     =>  Opcodes.Main[op]
      };
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Opcode DecodeCBInstruction()
    {
      var op = FetchByte();
      return Opcodes.CB[op];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Opcode DecodeDDInstruction()
    {
      var op = FetchByte();

      if (op != 0xCB)
        return Opcodes.DD[op];
      
      op = FetchByte();
      return Opcodes.DDCB[op];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Opcode DecodeEDInstruction()
    {
      var op = FetchByte();
      return Opcodes.ED[op];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Opcode DecodeFDInstruction()
    {
      var op = FetchByte();

      if (op != 0xCB)
        return Opcodes.FD[op];
        
      op = FetchByte();
      return Opcodes.FDCB[op];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteInstruction()
    {
      switch (_instruction.Operation)
      {
        case Operation.ADC8:  ADC8();   return;
        case Operation.ADC16: ADC16();  return;
        case Operation.ADD8:  ADD8();   return;
        case Operation.ADD16: ADD16();  return;
        case Operation.AND:   AND();    return;
        case Operation.BIT:   BIT();    return;
        case Operation.CALL:  CALL();   return;
        case Operation.CCF:   CCF();    return;
        case Operation.CP:    CP();     return;
        case Operation.CPD:   CPD();    return;
        case Operation.CPI:   CPI();    return;
        case Operation.CPIR:  CPIR();   return;
        case Operation.CPL:   CPL();    return;
        case Operation.DAA:   DAA();    return;
        case Operation.DEC8:  DEC8();   return;
        case Operation.DEC16: DEC16();  return;
        case Operation.DI:    DI();     return;
        case Operation.DJNZ:  DJNZ();   return;
        case Operation.EI:    EI();     return;
        case Operation.EX:    EX();     return;
        case Operation.EXX:   EXX();    return;
        case Operation.HALT:  HALT();   return;
        case Operation.IM:    IM();     return;
        case Operation.IN:    IN();     return;
        case Operation.INC8:  INC8();   return;
        case Operation.INC16: INC16();  return;
        case Operation.IND:   IND();    return;
        case Operation.INI:   INI();    return;
        case Operation.INIR:  INIR();   return;
        case Operation.JP:    JP();     return;
        case Operation.JR:    JR();     return;
        case Operation.LD8:   LD8();    return;
        case Operation.LD16:  LD16();   return;
        case Operation.NEG:   NEG();    return;
        case Operation.NOP:   NOP();    return;
        case Operation.OR:    OR();     return;
        case Operation.OTDR:  OTDR();   return;
        case Operation.OTIR:  OTIR();   return;
        case Operation.OUT:   OUT();    return;
        case Operation.OUTD:  OUTD();   return;
        case Operation.OUTI:  OUTI();   return;
        case Operation.POP:   POP();    return;
        case Operation.PUSH:  PUSH();   return;
        case Operation.RES0:  RES(0);   return;
        case Operation.RES1:  RES(1);   return;
        case Operation.RES2:  RES(2);   return;
        case Operation.RES3:  RES(3);   return;
        case Operation.RES4:  RES(4);   return;
        case Operation.RES5:  RES(5);   return;
        case Operation.RES6:  RES(6);   return;
        case Operation.RES7:  RES(7);   return;
        case Operation.RL:    RL();     return;
        case Operation.RLA:   RLA();    return;
        case Operation.RLC:   RLC();    return;
        case Operation.RLCA:  RLCA();   return;
        case Operation.RLD:   RLD();    return;
        case Operation.RR:    RR();     return;
        case Operation.RRA:   RRA();    return;
        case Operation.RRC:   RRC();    return;
        case Operation.RRCA:  RRCA();   return;
        case Operation.RRD:   RRD();    return;
        case Operation.RST:   RST();    return;
        case Operation.SBC8:  SBC8();   return;
        case Operation.SBC16: SBC16();  return;
        case Operation.SCF:   SCF();    return;
        case Operation.SET0:  SET(0);   return;
        case Operation.SET1:  SET(1);   return;
        case Operation.SET2:  SET(2);   return;
        case Operation.SET3:  SET(3);   return;
        case Operation.SET4:  SET(4);   return;
        case Operation.SET5:  SET(5);   return;
        case Operation.SET6:  SET(6);   return;
        case Operation.SET7:  SET(7);   return;
        case Operation.SLA:   SLA();    return;
        case Operation.SRA:   SRA();    return;
        case Operation.SRL:   SRL();    return;
        case Operation.SUB:   SUB();    return;
        case Operation.XOR:   XOR();    return;
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte FetchByte() => _memory.ReadByte(_pc++);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort FetchWord()
    {
      var lowByte = FetchByte();
      var highByte = FetchByte();
      return highByte.Concat(lowByte);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte ReadByteOperand(Operand operand)
    {
      switch (operand)
      {
        case Operand.Indirect:
          _memPtr = FetchWord();
          break;

        case Operand.BCi: _memPtr = _bc; break;
        case Operand.DEi: _memPtr = _de; break;
        case Operand.HLi: _memPtr = _hl; break;

        case Operand.IXd: _memPtr = (ushort)(_ix + FetchByte()); break;
        case Operand.IYd: _memPtr = (ushort)(_iy + FetchByte()); break;

        case Operand.Immediate:
          return FetchByte();

        case Operand.A: return _a;
        case Operand.B: return _b;
        case Operand.C: return _c;
        case Operand.D: return _d;
        case Operand.E: return _e;
        case Operand.F: return (byte)_flags;
        case Operand.H: return _h;
        case Operand.L: return _l;
        default: throw new Exception($"Invalid byte operand: {_instruction.Destination}");
      }
      return _memory.ReadByte(_memPtr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort ReadWordOperand(Operand operand)
    {
      switch (operand)
      {
        case Operand.Indirect:
          _memPtr = FetchWord();
          break;

        case Operand.Immediate:
          return FetchWord();

        case Operand.AF: return _af;
        case Operand.BC: return _bc;
        case Operand.DE: return _de;
        case Operand.HL: return _hl;
        case Operand.IX: return _ix;
        case Operand.IY: return _iy;
        case Operand.SP: return _sp;
        default: throw new Exception($"Invalid word operand: {_instruction.Destination}");
      }
      return _memory.ReadByte(_memPtr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteByteResult(byte value) => WriteByteResult(value, _instruction.Destination);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteByteResult(byte value, Operand destination)
    {
      switch (destination)
      {
        case Operand.A: _a = value; return;
        case Operand.B: _b = value; return;
        case Operand.C: _c = value; return;
        case Operand.D: _d = value; return;
        case Operand.E: _e = value; return;
        case Operand.H: _h = value; return;
        case Operand.L: _l = value; return;

        case Operand.Indirect:  
          _memPtr = FetchWord();
          break;

        case Operand.IXd:
          _memPtr = (byte)(_ix + FetchByte());
          break;

        case Operand.IYd:
          _memPtr = (byte)(_iy + FetchByte());
          break;

        case Operand.BCi: _memPtr = _bc; break;
        case Operand.DEi: _memPtr = _de; break;
        case Operand.HLi: _memPtr = _hl; break;
        default: throw new Exception($"Invalid byte destination: {destination}");
      }
      _memory.WriteByte(_memPtr, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteWordResult(ushort value)
    {
      switch (_instruction.Destination)
      {
        case Operand.Indirect:
          _memory.WriteWord(_memPtr, value);
          return;

        case Operand.AF: _af = value; return;
        case Operand.BC: _bc = value; return;
        case Operand.DE: _de = value; return;
        case Operand.HL: _hl = value; return;
        case Operand.IX: _ix = value; return;
        case Operand.IY: _iy = value; return;
        case Operand.SP: _sp = value; return;
        default: throw new Exception($"Invalid word destination: {_instruction.Destination}");
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool EvaluateCondition() => _instruction.Source switch
    {
      Operand.Carry     => _carry,
      Operand.Zero      => _zero,
      Operand.Negative  => _sign,
      Operand.Even      => _parity,
      Operand.NonCarry  => !_carry,
      Operand.NonZero   => !_zero,
      Operand.Positive  => !_sign,
      Operand.Odd       => !_parity,
      Operand.Implied   => true,
      _ => throw new Exception($"Invalid condition: {_instruction.Source}")
    };

    public void DumpMemory(string path) => _memory.DumpRAM(path);
    public void DumpROM(string path) => _memory.DumpROM(path);

    public override String ToString() => $"{DumpRegisters()}Flags: {_flags} | CIR: {_instruction} | Cycle: {_instructionCount}\r\n{_memory}";
  }
}