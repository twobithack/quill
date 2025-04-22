using System;
using System.Numerics;
using System.Runtime.CompilerServices;

using Quill.Common.Extensions;
using Quill.CPU.Definitions;
using Quill.IO;
using Quill.Sound;
using Quill.Video;

namespace Quill.CPU;

unsafe public ref partial struct Z80
{
  public Z80(byte[] rom, PSG sound, VDP video, Ports io)
  {
    _flags = Flags.None;
    _instruction = Opcodes.Main[0x00];
    _memory = new Memory(rom);
    _psg = sound;
    _vdp = video;
    _ports = io;
  }

  #region Methods
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public int Step()
  {
    HandleInterrupts();
    if (_halt)
    {
      _r++;
      return 0x04;
    }
    DecodeInstruction();
    ExecuteInstruction();
    return _instruction.Cycles;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void HandleInterrupts()
  {
    if (_ports.NMI)
    {
      PushToStack(_pc);
      _pc = 0x66;
      _halt = false;
      _iff2 = _iff1;
      _iff1 = false;
      _ports.NMI = false;
    }

    if (_eiPending)
    {
      _iff1 = true;
      _iff2 = true;
      _eiPending = false;
      return;
    }

    if (_iff1 && _vdp.IRQ)
    {
      PushToStack(_pc);
      _pc = 0x38;
      _halt = false;
      _iff1 = false;
      _iff2 = false;
      _r++;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private byte FetchByte() => _memory.ReadByte(_pc++);
    
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private sbyte FetchSignedByte() => (sbyte)FetchByte();

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private ushort FetchWord()
  {
    var lowByte = FetchByte();
    var highByte = FetchByte();
    return highByte.Concat(lowByte);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void PushToStack(ushort value)
  {
    _sp -= 2;
    _memory.WriteWord(_sp, value);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private ushort PopFromStack()
  {
    var value = _memory.ReadWord(_sp);
    _sp += 2;
    return value;
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
  private Instruction DecodeCBInstruction()
  {
    _r++;
    return Opcodes.CB[FetchByte()];
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private Instruction DecodeDDInstruction()
  {
    _r++;
    var op = FetchByte();
    if (op != 0xCB)
      return Opcodes.DD[op];
    
    _memPtr = (ushort)(IX + FetchSignedByte());
    return Opcodes.DDCB[FetchByte()];
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private Instruction DecodeEDInstruction()
  {
    _r++;
    return Opcodes.ED[FetchByte()];
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private Instruction DecodeFDInstruction()
  {
    _r++;
    var op = FetchByte();
    if (op != 0xCB)
      return Opcodes.FD[op];
      
    _memPtr = (ushort)(IY + FetchSignedByte());
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
      case Operation.IM:    IM();     return;
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
    ushort address;
    switch (operand)
    {
      case Operand.A:   return _a;
      case Operand.B:   return _b;
      case Operand.C:   return _c;
      case Operand.D:   return _d;
      case Operand.E:   return _e;
      case Operand.F:   return (byte)_flags;
      case Operand.H:   return _h;
      case Operand.L:   return _l;
      case Operand.I:   return _i;
      case Operand.R:   return _r;
      case Operand.IXh: return _ixh;
      case Operand.IXl: return _ixl;
      case Operand.IYh: return _iyh;
      case Operand.IYl: return _iyl;

      case Operand.Immediate:
        if (_instruction.Destination == Operand.IXd)
          _memPtr = (ushort)(IX + FetchSignedByte());
        else if (_instruction.Destination == Operand.IYd)
          _memPtr = (ushort)(IY + FetchSignedByte());
        return FetchByte();

      case Operand.Indirect:  address = FetchWord();  break;
      case Operand.BCi:       address = BC;           break;
      case Operand.DEi:       address = DE;           break;
      case Operand.HLi:       address = HL;           break;

      case Operand.IXd:
        _memPtr ??= (ushort)(IX + FetchSignedByte());
        address = _memPtr.Value;
        break;

      case Operand.IYd:
        _memPtr ??= (ushort)(IY + FetchSignedByte());
        address = _memPtr.Value;
        break;

      default: throw new Exception($"Invalid byte operand: {_instruction}");
    }
    return _memory.ReadByte(address);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private ushort ReadWordOperand(Operand operand)
  {
    ushort address;
    switch (operand)
    {
      case Operand.Immediate: return FetchWord();
      case Operand.AF:        return AF;
      case Operand.BC:        return BC;
      case Operand.DE:        return DE;
      case Operand.HL:        return HL;
      case Operand.IX:        return IX;
      case Operand.IY:        return IY;
      case Operand.SP:        return _sp;

      case Operand.Indirect: 
        address = FetchWord();
        break;

      default: throw new Exception($"Invalid word operand: {_instruction}");
    }
    return _memory.ReadWord(address); 
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private readonly byte ReadPort(byte port) => port switch
  {
    0x3E => 0xFF,
    0x3F => 0xFF,
    0x7E => _vdp.VCounter,
    0x7F => _vdp.HCounter,
    0xBE => _vdp.ReadData(),
    0xBF or 0xBD => _vdp.ReadStatus(),
    0xDC or 0xC0 => _ports.ReadPortA(),
    0xDD or 0xC1 => _ports.ReadPortB(),
    byte mirror when mirror < 0x3E => 0xFF,
    byte mirror when mirror > 0x3F &&
                     mirror < 0x80 &&
                     mirror % 2 == 0 => _vdp.VCounter,
    byte mirror when mirror > 0x3F &&
                     mirror < 0x80 &&
                     mirror % 2 != 0 => _vdp.HCounter,
    byte mirror when mirror > 0x7F &&
                     mirror < 0xC0 &&
                     mirror % 2 == 0 => _vdp.ReadData(),
    byte mirror when mirror > 0x7F &&
                     mirror < 0xC0 &&
                     mirror % 2 != 0 => _vdp.ReadStatus(),
    byte mirror when mirror > 0xC0 &&
                     mirror % 2 == 0 => _ports.ReadPortA(),
    byte mirror when mirror > 0xC1 &&
                     mirror % 2 != 0 => _ports.ReadPortB(),
    _ => 0xFF
  };

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void WriteByteResult(byte value) => WriteByteResult(value, _instruction.Destination);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void WriteByteResult(byte value, Operand destination)
  {
    ushort address;
    switch (destination)
    { 
      case Operand.A:   _a = value;   return;
      case Operand.B:   _b = value;   return;
      case Operand.C:   _c = value;   return;
      case Operand.D:   _d = value;   return;
      case Operand.E:   _e = value;   return;
      case Operand.H:   _h = value;   return;
      case Operand.L:   _l = value;   return;
      case Operand.I:   _i = value;   return;
      case Operand.R:   _r = value;   return;
      case Operand.IXh: _ixh = value; return;
      case Operand.IXl: _ixl = value; return;
      case Operand.IYh: _iyh = value; return;
      case Operand.IYl: _iyl = value; return;

      case Operand.Indirect:  address = FetchWord();  break;
      case Operand.BCi:       address = BC;           break;
      case Operand.DEi:       address = DE;           break;
      case Operand.HLi:       address = HL;           break;

      case Operand.IXd:
        address = _memPtr ?? (ushort)(IX + FetchSignedByte());
        break;

      case Operand.IYd: 
        address = _memPtr ?? (ushort)(IY + FetchSignedByte());
        break;

      default: throw new Exception($"Invalid byte destination: {destination}");
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

      case Operand.AF:  AF = value;   return;
      case Operand.BC:  BC = value;   return;
      case Operand.DE:  DE = value;   return;
      case Operand.HL:  HL = value;   return;
      case Operand.IX:  IX = value;   return;
      case Operand.IY:  IY = value;   return;
      case Operand.SP:  _sp = value;  return;
      
      default: throw new Exception($"Invalid word destination: {_instruction}");
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private readonly void WritePort(byte port, byte value)
  {
    switch (port)
    {
      case 0x7E:
      case 0x7F:
        _psg.WriteData(value);
        return;

      case 0xBD:
      case 0xBF:
        _vdp.WriteControl(value);
        return;

      case 0xBE:
        _vdp.WriteData(value);
        return;
    
      case 0x3E:
      case byte mirror when mirror < 0x3E &&
                            mirror % 2 == 0:
        // Memory controller
        return;
    
      case 0x3F:
      case byte mirror when mirror < 0x3E &&
                            mirror % 2 != 0:
        _ports.WriteControl(value);
        return;

      case byte mirror when mirror > 0x3F &&
                            mirror < 0x80:
        _psg.WriteData(value);
        return;

      case byte mirror when mirror > 0x7F &&
                            mirror < 0xC0 &&
                            mirror % 2 != 0:
        _vdp.WriteControl(value);
        return;

      case byte mirror when mirror > 0x7F &&
                            mirror < 0xC0 &&
                            mirror % 2 == 0:
        _vdp.WriteData(value);
        return;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private readonly bool EvaluateCondition() => _instruction.Source switch
  {
    Operand.Carry     => CarryFlag,
    Operand.NonCarry  => !CarryFlag,
    Operand.Zero      => ZeroFlag,
    Operand.NonZero   => !ZeroFlag,
    Operand.Negative  => SignFlag,
    Operand.Positive  => !SignFlag,
    Operand.Even      => ParityFlag,
    Operand.Odd       => !ParityFlag,
    Operand.Implied   => true,

    _ => throw new Exception($"Invalid condition: {_instruction}")
  };

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static bool CheckByteOverflow(int a, int b, int result)
  {
    var carry = a ^ b ^ result;
    var carryIn = (carry >> 7) & 1;
    var carryOut = (carry >> 8) & 1;
    return (carryIn != carryOut);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static bool CheckWordOverflow(int a, int b, int result)
  {
    var carry = a ^ b ^ result;
    var carryIn = (carry >> 15) & 1;
    var carryOut = (carry >> 16) & 1;
    return (carryIn != carryOut);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static bool CheckParity(uint value) => BitOperations.PopCount(value) % 2 == 0;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private readonly bool GetFlag(Flags flag) => (_flags & flag) != 0;
  private void SetFlag(Flags flag, bool value) => _flags = value
                                                ? _flags | flag 
                                                : _flags & ~flag;
  #endregion
}