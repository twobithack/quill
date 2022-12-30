using Quill.Common;
using Quill.CPU.Definitions;
using Quill.Input;
using Quill.Video;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static Quill.CPU.Definitions.Opcodes;

namespace Quill.CPU;

unsafe public ref partial struct Z80
{
  #region Fields
  private const byte NOP_CYCLES = 0x04;
  private Memory _memory;
  private VDP _vdp;
  private Joypads _input;
  private Flags _flags;
  private bool _halt = false;
  private bool _iff1 = true;
  private bool _iff2 = true;
  private byte _a = 0x00;
  private byte _b = 0x00;
  private byte _c = 0x00;
  private byte _d = 0x00;
  private byte _e = 0x00;
  private byte _h = 0x00;
  private byte _l = 0x00;
  private byte _r = 0x00;
  private ushort _pc = 0x0000;
  private ushort _sp = 0x0000;
  private ushort _ix = 0x0000;
  private ushort _iy = 0x0000;
  private ushort _afShadow = 0x0000;
  private ushort _bcShadow = 0x0000;
  private ushort _deShadow = 0x0000;
  private ushort _hlShadow = 0x0000;
  private ushort? _memPtr = null;
  private Opcode _instruction;
  #endregion

  public Z80(byte[] rom, VDP vdp, Joypads input)
  {
    _flags = Flags.None;
    _instruction = new Opcode();
    _memory = new Memory(rom);
    _vdp = vdp;
    _input = input;
  }

  #region Properties
  private bool _signFlag
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => _flags.HasFlag(Flags.Sign);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => SetFlag(Flags.Sign, value);
  }

  private bool _zeroFlag
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => _flags.HasFlag(Flags.Zero);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => SetFlag(Flags.Zero, value);
  }

  private bool _halfcarryFlag
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => _flags.HasFlag(Flags.Halfcarry);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => SetFlag(Flags.Halfcarry, value);
  }

  private bool _parityFlag
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => _flags.HasFlag(Flags.Parity);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => SetFlag(Flags.Parity, value);
  }

  private bool _negativeFlag
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => _flags.HasFlag(Flags.Negative);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => SetFlag(Flags.Negative, value);
  }

  private bool _carryFlag
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => _flags.HasFlag(Flags.Carry);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => SetFlag(Flags.Carry, value);
  }

  private ushort _af 
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => _a.Concat((byte)_flags);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set
    {
      _a = value.HighByte();
      _flags = (Flags)value.LowByte();
    }
  }

  private ushort _bc
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => _b.Concat(_c);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set
    {
      _b = value.HighByte();
      _c = value.LowByte();
    }
  }

  private ushort _de
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => _d.Concat(_e);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set
    {
      _d = value.HighByte();
      _e = value.LowByte();
    }
  }

  private ushort _hl
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => _h.Concat(_l);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set
    {
      _h = value.HighByte();
      _l = value.LowByte();
    }
  }
  
  private byte _ixh
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => _ix.HighByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => _ix = value.Concat(_ixl);
  }

  private byte _ixl
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => _ix.LowByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => _ix = _ixh.Concat(value);
  }

  private byte _iyh
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => _iy.HighByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => _iy = value.Concat(_iyl);
  }

  private byte _iyl
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => _iy.LowByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => _iy = _iyh.Concat(value);
  }
  #endregion

  public byte Step()
  {
    HandleInterrupts();

    if (_halt)
    {
      _r++;
      return NOP_CYCLES;
    }

    DecodeInstruction();
    ExecuteInstruction();
    return _instruction.Cycles;
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void HandleInterrupts()
  {
    if (_iff1 && _vdp.IRQ)
    {
      _halt = false;
      _iff1 = false;
      _iff2 = false;
      _r++;

      _sp -= 2;
      _memory.WriteWord(_sp, _pc);
      _pc = 0x38;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private byte FetchByte() => _memory.ReadByte(_pc++);
    
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private sbyte FetchDisplacement() => (sbyte)FetchByte();

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
    _memPtr = null;
    _instruction = op switch
    {
      0xCB  =>  DecodeCBInstruction(),
      0xDD  =>  DecodeDDInstruction(),
      0xED  =>  DecodeEDInstruction(),
      0xFD  =>  DecodeFDInstruction(),
      _     =>  Opcodes.Main[op]
    };
    _r++;
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private Opcode DecodeCBInstruction()
  {
    _r++;
    return Opcodes.CB[FetchByte()];
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private Opcode DecodeDDInstruction()
  {
    _r++;
    var op = FetchByte();
    if (op != 0xCB)
      return Opcodes.DD[op];
    
    _memPtr = (ushort)(_ix + FetchDisplacement());
    return Opcodes.DDCB[FetchByte()];
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private Opcode DecodeEDInstruction()
  {
    _r++;
    return Opcodes.ED[FetchByte()];
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private Opcode DecodeFDInstruction()
  {
    _r++;
    var op = FetchByte();
    if (op != 0xCB)
      return Opcodes.FD[op];
      
    _memPtr = (ushort)(_iy + FetchDisplacement());
    return Opcodes.FDCB[FetchByte()];
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
      case Operation.IM:              return;
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
      case Operation.LDI:   LDI();    return;
      case Operation.LDIR:  LDIR();   return;
      case Operation.NEG:   NEG();    return;
      case Operation.NOP:             return;
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
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private byte ReadByteOperand(Operand operand)
  {
    ushort address = 0x0000;
    switch (operand)
    {
      case Operand.Immediate:
        if (_instruction.Destination == Operand.IXd)
          _memPtr = (ushort)(_ix + FetchDisplacement());
        else if (_instruction.Destination == Operand.IYd)
          _memPtr = (ushort)(_iy + FetchDisplacement());
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
        address = FetchWord();
        break;

      case Operand.BCi: address = _bc; break;
      case Operand.DEi: address = _de; break;
      case Operand.HLi: address = _hl; break;

      case Operand.IXd:
        _memPtr ??= (ushort)(_ix + FetchDisplacement());
        address = _memPtr.Value;
        break;

      case Operand.IYd:
        _memPtr ??= (ushort)(_iy + FetchDisplacement());
        address = _memPtr.Value;
        break;

      #if DEBUG
      default: throw new Exception($"Invalid byte operand: {_instruction}");
      #endif
    }
    return _memory.ReadByte(address);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private ushort ReadWordOperand(Operand operand)
  {
    ushort address = 0x0000;
    switch (operand)
    {
      case Operand.Indirect: 
        address = FetchWord();
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

      #if DEBUG
      default: throw new Exception($"Invalid word operand: {_instruction}");
      #endif
    }
    return _memory.ReadWord(address); 
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private byte ReadPort(byte port) => port switch
  {
    0x7E => _vdp.VCounter,
    0x7F => _vdp.HCounter,
    0xBE => _vdp.ReadData(),
    0xBF or 0xBD => _vdp.ReadStatus(),
    0xDC or 0xC0 => _input.ReadPortA(),
    0xDD or 0xC1 => _input.ReadPortB(),

    #if DEBUG
    _ => throw new Exception($"Unable to read port: {port}\r\n{this.ToString()}")
    #endif
  };

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void WriteByteResult(byte value) => WriteByteResult(value, _instruction.Destination);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void WriteByteResult(byte value, Operand destination)
  {
    ushort address = 0x0000;
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

      case Operand.BCi: address = _bc; break;
      case Operand.DEi: address = _de; break;
      case Operand.HLi: address = _hl; break;
      case Operand.Indirect: address = FetchWord(); break;

      case Operand.IXd:
        address = _memPtr ?? (ushort)(_ix + FetchDisplacement());
        break;

      case Operand.IYd: 
        address = _memPtr ?? (ushort)(_iy + FetchDisplacement());
        break;

      #if DEBUG
      default: throw new Exception($"Invalid byte destination: {destination}");
      #endif
    }
    _memory.WriteByte(address, value);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void WriteWordResult(ushort value)
  {
    switch (_instruction.Destination)
    {
      case Operand.Indirect:
        var address = FetchWord();
        _memory.WriteWord(address, value);
        return;

      case Operand.AF: _af = value; return;
      case Operand.BC: _bc = value; return;
      case Operand.DE: _de = value; return;
      case Operand.HL: _hl = value; return;
      case Operand.IX: _ix = value; return;
      case Operand.IY: _iy = value; return;
      case Operand.SP: _sp = value; return;
      
      #if DEBUG
      default: throw new Exception($"Invalid word destination: {_instruction}");
      #endif
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void WritePort(byte port, byte value)
  {
    switch (port)
    {
      case 0x7E:
      case 0x7F: 
        // Sound chip
        return;

      case 0xBD:
      case 0xBF:
        _vdp.WriteControl(value);
        return;

      case 0xBE:
        _vdp.WriteData(value);
        return;

      case 0x3E:
        // IO controller
        return;

      #if DEBUG
      case 0xFC:
        ControlSDSC(value);
        return;

      case 0xFD:
        WriteSDSC(value);
        return;

      default: 
        throw new Exception($"Unable to write to port {port.ToHex()}\r\n{this.ToString()}");
      #endif
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private bool EvaluateCondition() => _instruction.Source switch
  {
    Operand.Carry     => _carryFlag,
    Operand.Zero      => _zeroFlag,
    Operand.Negative  => _signFlag,
    Operand.Even      => _parityFlag,
    Operand.NonCarry  => !_carryFlag,
    Operand.NonZero   => !_zeroFlag,
    Operand.Positive  => !_signFlag,
    Operand.Odd       => !_parityFlag,
    Operand.Implied   => true,

    #if DEBUG
    _ => throw new Exception($"Invalid condition: {_instruction}")
    #endif
  };

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SetFlag(Flags flag, bool value) => _flags = value
                                                          ? _flags | flag 
                                                          : _flags & ~flag;

  public string DumpRegisters()
  {
    return "╒══════════╤══════════╤══════════╤══════════╤═══════════╕\r\n" +
           $"│ PC: {_pc.ToHex()} │ SP: {_sp.ToHex()} │ IX: {_ix.ToHex()} │ IY: {_iy.ToHex()} │ R: {_r.ToHex()}     │\r\n" +
           $"│ AF: {_af.ToHex()} │ BC: {_bc.ToHex()} │ DE: {_de.ToHex()} │ HL: {_hl.ToHex()} │ IFF1: {_iff1.ToBit()}   │\r\n" +
           $"│     {_afShadow.ToHex()} │     {_bcShadow.ToHex()} │     {_deShadow.ToHex()} │     {_hlShadow.ToHex()} │ IFF2: {_iff2.ToBit()}   │\r\n" +
           "╘══════════╧══════════╧══════════╧══════════╧═══════════╛\r\n";
  }
  
  public void DumpMemory(string path) => _memory.DumpRAM(path);
  public void DumpROM(string path) => _memory.DumpROM(path);

  public override string ToString() => $"{DumpRegisters()}Flags: {_flags} | CIR: {_instruction}\r\n{_memory.ToString()}\r\n{_vdp.ToString()}\r\n";
}