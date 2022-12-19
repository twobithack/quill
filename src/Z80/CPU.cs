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
      }
      return _memory.ReadByte(_memPtr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteByteResult(byte value)
    {
      switch (_instruction.Destination)
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
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool EvaluationCondition() => _instruction.Source switch
    {
      Operand.Carry     => _carry,
      Operand.Zero      => _zero,
      Operand.Negative  => _sign,
      Operand.Even      => _parity,
      Operand.NonCarry  => !_carry,
      Operand.NonZero   => !_zero,
      Operand.Positive  => !_sign,
      Operand.Odd       => !_parity,
      _                 => true
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort GetBitIndex() => _instruction.Destination switch
    {
      Operand.Bit0  => 0,
      Operand.Bit1  => 1,
      Operand.Bit2  => 2,
      Operand.Bit3  => 3,
      Operand.Bit4  => 4,
      Operand.Bit5  => 5,
      Operand.Bit6  => 6,
      Operand.Bit7  => 7
    };

    public void DumpMemory(string path) => _memory.Dump(path);
    public void DumpROM(string path) => _memory.DumpROM(path);

    public override String ToString() => $"{DumpRegisters()}Flags: {_flags} | CIR: {_instruction} | Cycle: {_instructionCount}\r\n{_memory}";
  }
}