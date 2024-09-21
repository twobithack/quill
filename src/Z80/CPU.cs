using Quill.Definitions;
using Quill.Extensions;
using static Quill.Definitions.Opcodes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Quill
{
  unsafe public ref partial struct CPU
  {
    public CPU(byte[] rom, VDP vdp)
    {
      _instruction = new Opcode();
      _memory = new Memory(rom);
      _vdp = vdp;
    }

    public void Step()
    {
      HandleInterrupts();
      DecodeInstruction();
      ExecuteInstruction();
      _cycleCount += _instruction.Cycles;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HandleInterrupts()
    {
      if (_halt)
        throw new Exception($"Halted\r\n{this.ToString()}");

      if (_iff1 && _vdp.IRQ)
      {
        _halt = false;
        _iff1 = false;
        _iff2 = false;
        _r++;

        _memory.WriteWord(_sp, _pc);
        _sp += 2;
        _pc = 0x38;
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
    private void DecodeInstruction()
    {
      var op = FetchByte();
      _r++;

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
      _r++;

      return Opcodes.CB[op];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Opcode DecodeDDInstruction()
    {
      var op = FetchByte();
      _r++;

      if (op != 0xCB)
        return Opcodes.DD[op];
      
      op = FetchByte();
      return Opcodes.DDCB[op];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Opcode DecodeEDInstruction()
    {
      var op = FetchByte();
      _r++;
      
      return Opcodes.ED[op];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Opcode DecodeFDInstruction()
    {
      var op = FetchByte();
      _r++;

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
        case Operation.CPDR:  CPDR();   return;
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
        case Operation.IM:    return;
        case Operation.IN:    IN();     return;
        case Operation.INC8:  INC8();   return;
        case Operation.INC16: INC16();  return;
        case Operation.IND:   IND();    return;
        case Operation.INDR:  INDR();   return;
        case Operation.INI:   INI();    return;
        case Operation.INIR:  INIR();   return;
        case Operation.JP:    JP();     return;
        case Operation.JR:    JR();     return;
        case Operation.LD8:   LD8();    return;
        case Operation.LD16:  LD16();   return;
        case Operation.LDD:   LDD();    return;
        case Operation.LDDR:  LDDR();   return;
        case Operation.NEG:   NEG();    return;
        case Operation.NOP:   return;
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
        case Operation.RET:   RET();    return;
        case Operation.RETI:  RETI();   return;
        case Operation.RETN:  RETN();   return;
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
        case Operation.SLL:   SLL();    return;
        case Operation.SRA:   SRA();    return;
        case Operation.SRL:   SRL();    return;
        case Operation.SUB:   SUB();    return;
        case Operation.XOR:   XOR();    return;
        // default: throw new Exception($"Not found: {_instruction}");
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte ReadByteOperand(Operand operand)
    {
      switch (operand)
      {
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
        case Operand.IXh: return _ixh;
        case Operand.IXl: return _ixl;
        case Operand.IYh: return _iyh;
        case Operand.IYl: return _iyl;

        case Operand.Indirect:
          _addressBus = FetchWord();
          break;

        case Operand.BCi: _addressBus = _bc; break;
        case Operand.DEi: _addressBus = _de; break;
        case Operand.HLi: _addressBus = _hl; break;

        case Operand.IXd: _addressBus = (ushort)(_ix + FetchByte()); break;
        case Operand.IYd: _addressBus = (ushort)(_iy + FetchByte()); break;

        //default: throw new Exception($"Invalid byte operand: {_instruction}");
      }
      return _memory.ReadByte(_addressBus);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort ReadWordOperand(Operand operand)
    {
      switch (operand)
      {
        case Operand.Immediate:
          return FetchWord();

        case Operand.AF: return _af;
        case Operand.BC: return _bc;
        case Operand.DE: return _de;
        case Operand.HL: return _hl;
        case Operand.IX: return _ix;
        case Operand.IY: return _iy;
        case Operand.SP: return _sp;

        case Operand.Indirect: 
          _addressBus = FetchWord();
          return _memory.ReadByte(_addressBus);

        default: throw new Exception($"Invalid word operand: {_instruction}");
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte ReadPort(byte port) => port switch
    {
      0x7E => _vdp.VCounter,
      0x7F => _vdp.HCounter,
      0xBE => _vdp.Data,
      0xBF or 0xBD => _vdp.Status,
      0xDC or 0xC0 => 0xDC, // joypad 1
      0xDD or 0xC1 => 0xDD, // joypad 2
      _ => throw new Exception($"Unable to read port: {port}\r\n{this.ToString()}")
    };

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
        case Operand.IXh: _ixh = value; return;
        case Operand.IXl: _ixl = value; return;
        case Operand.IYh: _iyh = value; return;
        case Operand.IYl: _iyl = value; return;

        case Operand.BCi: _addressBus = _bc; break;
        case Operand.DEi: _addressBus = _de; break;
        case Operand.HLi: _addressBus = _hl; break;
        case Operand.IXd: _addressBus = (byte)(_ix + FetchByte()); break;
        case Operand.IYd: _addressBus = (byte)(_iy + FetchByte()); break;
        case Operand.Indirect: _addressBus = FetchWord(); break;

        //default: throw new Exception($"Invalid byte destination: {destination}");
      }
      _memory.WriteByte(_addressBus, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteWordResult(ushort value)
    {
      switch (_instruction.Destination)
      {
        case Operand.Indirect:
          _memory.WriteWord(_addressBus, value);
          return;

        case Operand.AF: _af = value; return;
        case Operand.BC: _bc = value; return;
        case Operand.DE: _de = value; return;
        case Operand.HL: _hl = value; return;
        case Operand.IX: _ix = value; return;
        case Operand.IY: _iy = value; return;
        case Operand.SP: _sp = value; return;
        //default: throw new Exception($"Invalid word destination: {_instruction}");
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WritePort(byte port, byte value)
    {
      switch (port)
      {
        case 0x7E:
        case 0x7F: 
          // TODO: write to sound chip
           return;

        case 0xBE:
          _vdp.Data = value;
          return;

        case 0xBF:
        case 0xBD:
          _vdp.Control = value;
          return;

        default:
          throw new Exception($"Unable to write to port {port}\r\n{this.ToString()}");
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
      //_ => throw new Exception($"Invalid condition: {_instruction}")
    };

    public void DumpMemory(string path) => _memory.DumpRAM(path);
    public void DumpROM(string path) => _memory.DumpROM(path);

    public override String ToString() => $"{DumpRegisters()}Flags: {_flags} | CIR: {_instruction} | Cycle: {_cycleCount}\r\n{_memory.ToString()}";
  }
}