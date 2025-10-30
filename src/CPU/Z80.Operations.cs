using System;
using System.Runtime.CompilerServices;

using Quill.Common.Extensions;
using Quill.CPU.Definitions;

namespace Quill.CPU;

unsafe public ref partial struct Z80
{
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void ADC8()
  {
    var addend = ReadByteOperand(_instruction.Source);
    var carry = CarryFlag ? 1 : 0;
    var sum = _a + addend + carry;
    
    var flags = (Flags)(sum & 0b_1010_1000);
    if ((byte)sum == 0)
      flags |= Flags.Zero;
    if (_a.LowNibble() + addend.LowNibble() + carry > 0xF)
      flags |= Flags.Halfcarry;
    if (CheckByteOverflow(_a, addend, sum))
      flags |= Flags.Parity;
    if (sum > byte.MaxValue)
      flags |= Flags.Carry;

    _a = (byte)sum;
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void ADC16()
  {
    var addend = ReadWordOperand(_instruction.Source);
    var carry = CarryFlag ? 1 : 0;
    var sum = HL + addend + carry;

    var flags = (Flags)((sum >> 8) & 0b_1010_1000);
    if ((ushort)sum == 0)
      flags |= Flags.Zero;
    if ((HL & 0xFFF) + (addend & 0xFFF) + carry > 0xFFF)
      flags |= Flags.Halfcarry;
    if (CheckWordOverflow(HL, addend, sum))
      flags |= Flags.Parity;
    if (sum > ushort.MaxValue)
      flags |= Flags.Carry;

    HL = (ushort)sum;
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void ADD8()
  {
    var addend = ReadByteOperand(_instruction.Source);
    var sum = _a + addend;

    var flags = (Flags)(sum & 0b_1010_1000);
    if ((byte)sum == 0)
      flags |= Flags.Zero;
    if (_a.LowNibble() + addend.LowNibble() > 0xF)
      flags |= Flags.Halfcarry;
    if (CheckByteOverflow(_a, addend, sum))
      flags |= Flags.Parity;
    if (sum > byte.MaxValue)
      flags |= Flags.Carry;

    _a = (byte)sum;
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void ADD16()
  {
    var augend = ReadWordOperand(_instruction.Destination);
    var addend = ReadWordOperand(_instruction.Source);
    var sum = augend + addend;

    var flags = (Flags)((byte)_flags & 0b_1110_1100);
    if ((augend & 0xFFF) + (addend & 0xFFF) > 0xFFF)
      flags |= Flags.Halfcarry;
    if (sum > ushort.MaxValue)
      flags |= Flags.Carry;

    WriteWordResult((ushort)sum);
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void AND()
  {
    var result = (byte)(_a & ReadByteOperand(_instruction.Source));

    var flags = (Flags)(result & 0b_1010_1000) | Flags.Halfcarry;
    if (result == 0)
      flags |= Flags.Zero;
    if (CheckParity(result))
      flags |= Flags.Parity;

    _a = result;
    _flags = flags;
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void BIT()
  {
    var value = ReadByteOperand(_instruction.Source);
    var index = (byte)_instruction.Destination;
    
    // TODO: Handle undocumented flags for HLi and indexed cases
    var flags = Flags.Halfcarry;
    if (!value.TestBit(index))
    {
      flags |= Flags.Zero;
      flags |= Flags.Parity;
    }
    else if (index == 3)
      flags |= Flags.X;
    else if (index == 5)
      flags |= Flags.Y;
    else if (index == 7)
      flags |= Flags.Sign;
    if (CarryFlag)
      flags |= Flags.Carry;

    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void CALL()
  {
    var address = FetchWord();
    if (!EvaluateCondition())
      return;

    PushToStack(_pc);
    _pc = address;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void CCF()
  {
    var flags = (Flags)((byte)_flags & 0b_1110_1100);
    if (CarryFlag)
      flags |= Flags.Halfcarry;
    else
      flags |= Flags.Carry;
    
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void CP()
  {
    var subtrahend = ReadByteOperand(_instruction.Source);
    var difference = _a - subtrahend;

    var flags = (Flags)(difference & 0b_1010_1000) | Flags.Negative;
    if ((byte)difference == 0)
      flags |= Flags.Zero;
    if (_a.LowNibble() < subtrahend.LowNibble())
      flags |= Flags.Halfcarry;
    if (CheckByteOverflow(_a, ~subtrahend, difference))
      flags |= Flags.Parity;
    if (_a < subtrahend)
      flags |= Flags.Carry;

    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void CPD()
  {
    var subtrahend = _bus.ReadByte(HL);
    var difference = _a - subtrahend;

    HL--;
    BC--;
          
    var flags = (Flags)(difference & 0b_1010_1000) | Flags.Negative;
    if (_a == subtrahend)
      flags |= Flags.Zero;
    if (_a.LowNibble() < subtrahend.LowNibble())
      flags |= Flags.Halfcarry;
    if (BC != 0)
      flags |= Flags.Parity;
    if (CarryFlag)
      flags |= Flags.Carry;

    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void CPDR()
  {
    CPD();
    if (ParityFlag && !ZeroFlag)
      _pc -= 2;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void CPI()
  {
    var subtrahend = _bus.ReadByte(HL);
    var difference = _a - subtrahend;

    HL++;
    BC--;
    
    var flags = (Flags)(difference & 0b_1010_1000) | Flags.Negative;
    if (_a == subtrahend)
      flags |= Flags.Zero;
    if (_a.LowNibble() < subtrahend.LowNibble())
      flags |= Flags.Halfcarry;
    if (BC != 0)
      flags |= Flags.Parity;
    if (CarryFlag)
      flags |= Flags.Carry;

    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void CPIR()
  {
    CPI();
    if (ParityFlag && !ZeroFlag)
      _pc -= 2;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void CPL()
  {
    _a = (byte)~_a;
    _flags = (Flags)(0b_1100_0101 & (byte)_flags) | 
             (Flags)(0b_0010_1000 & _a) | 
              Flags.Halfcarry | 
              Flags.Negative;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void DAA()
  {
    var value = _a;

    if (HalfcarryFlag || (_a & 0xF) > 9)
      value = NegativeFlag
            ? (byte)(value - 0x06)
            : (byte)(value + 0x06);

    if (CarryFlag || (_a > 0x99))
      value = NegativeFlag
            ? (byte)(value - 0x60)
            : (byte)(value + 0x60);

    var flags = (Flags)(value & 0b_1010_1000);
    if (value == 0)
      flags |= Flags.Zero;
    if (_a.TestBit(4) ^ value.TestBit(4))
      flags |= Flags.Halfcarry;
    if (CheckParity(value))
      flags |= Flags.Parity;
    if (NegativeFlag)
      flags |= Flags.Negative;
    if (CarryFlag | (_a > 0x99))
      flags |= Flags.Carry;

    _a = value;
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void DEC8()
  {
    var minuend = ReadByteOperand(_instruction.Destination);
    var difference = minuend - 1;

    var flags = (Flags)(difference & 0b_1010_1000) | Flags.Negative;
    if ((byte)difference == 0)
      flags |= Flags.Zero;
    if (minuend.LowNibble() == 0)
      flags |= Flags.Halfcarry;
    if (CheckByteOverflow(minuend, ~1, difference))
      flags |= Flags.Parity;
    if (CarryFlag)
      flags |= Flags.Carry;

    WriteByteResult((byte)difference);
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void DEC16()
  {
    var value = ReadWordOperand(_instruction.Destination);
    WriteWordResult(value.Decrement());
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void DI()
  {
    _iff1 = false;
    _iff2 = false;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void DJNZ()
  {
    var displacement = FetchSignedByte();
    if (--_b != 0)
      _pc = (ushort)(_pc + displacement);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void EI() => _eiPending = true;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void EX()
  {
    ushort temp;

    if (_instruction.Destination == Operand.AF)
    {
      temp = AF;
      AF = _afShadow;
      _afShadow = temp;
      return;
    }

    if (_instruction.Destination == Operand.DE)
    {
      temp = DE;
      DE = HL;
      HL = temp;
      return;
    }

    temp = _bus.ReadWord(_sp);
    switch (_instruction.Source)
    {
      case Operand.HL:
        _bus.WriteWord(_sp, HL);
        HL = temp;
        return;

      case Operand.IX:
        _bus.WriteWord(_sp, IX);
        IX = temp;
        return;

      case Operand.IY:
        _bus.WriteWord(_sp, IY);
        IY = temp;
        return;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void EXX()
  {
    ushort temp;

    temp = BC;
    BC = _bcShadow;
    _bcShadow = temp;

    temp = DE;
    DE = _deShadow;
    _deShadow = temp;

    temp = HL;
    HL = _hlShadow;
    _hlShadow = temp;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void HALT() => _halt = true;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void IM()
  {
    if (_instruction.Destination != (Operand)0x1)
      throw new Exception("Only interrupt mode 1 is supported.");
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void IN()
  {
    var port = ReadByteOperand(_instruction.Source);
    var value = ReadPort(port);

    if (_instruction.Destination != Operand.Implied)
      WriteByteResult(value);

    if (_instruction.Destination == Operand.A)
      return;

    var flags = (Flags)(value & 0b_1010_1000);
    if (value == 0)
      flags |= Flags.Zero;
    if (CheckParity(value))
      flags |= Flags.Parity;
    if (CarryFlag)
      flags |= Flags.Carry;

    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void INC8()
  {
    var augend = ReadByteOperand(_instruction.Destination);
    var sum = augend + 1;

    var flags = (Flags)(sum & 0b_1010_1000);
    if ((byte)sum == 0)
      flags |= Flags.Zero;
    if (augend.LowNibble() == 0xF)
      flags |= Flags.Halfcarry;
    if (CheckByteOverflow(augend, 1, sum))
      flags |= Flags.Parity;
    if (CarryFlag)
      flags |= Flags.Carry;

    WriteByteResult((byte)sum);
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void INC16()
  {
    var value = ReadWordOperand(_instruction.Destination);
    WriteWordResult(value.Increment());
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void IND()
  {
    var value = ReadPort(_c);
    _bus.WriteByte(HL, value);

    HL--;
    _b--;
    
    var flags = (Flags)(_b & 0b_1010_1000) | Flags.Negative;
    if (_b == 0)
      flags |= Flags.Zero;
    if (CarryFlag)
      flags |= Flags.Carry;

    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void INDR()
  {
    IND();
    if (!ZeroFlag)
      _pc -= 2;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void INI()
  {
    var value = ReadPort(_c);
    _bus.WriteByte(HL, value);

    HL++;
    _b--;
    
    var flags = (Flags)(_b & 0b_1010_1000) | Flags.Negative;
    if (_b == 0)
      flags |= Flags.Zero;
    if (CarryFlag)
      flags |= Flags.Carry;

    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void INIR()
  {
    INI();
    if (!ZeroFlag)
      _pc -= 2;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void JP()
  {
    var address = ReadWordOperand(_instruction.Destination);
    if (EvaluateCondition())
      _pc = address;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void JR()
  {
    var displacement = FetchSignedByte();
    if (EvaluateCondition()) 
      _pc = (ushort)(_pc + displacement);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void LD8()
  {
    var value = ReadByteOperand(_instruction.Source);
    WriteByteResult(value);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void LD16()
  {
    var value = ReadWordOperand(_instruction.Source);
    WriteWordResult(value);
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void LDD()
  {
    var value = _bus.ReadByte(HL);
    _bus.WriteByte(DE, value);
    
    DE--;
    HL--;
    BC--;
    
    var flags = (Flags)((byte)_flags & 0b_1110_1001);
    if (BC != 0) 
      flags |= Flags.Parity; 
    
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void LDDR()
  {
    LDD();
    if (ParityFlag)
      _pc -= 2;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void LDI()
  {
    var value = _bus.ReadByte(HL);
    _bus.WriteByte(DE, value);
    
    DE++;
    HL++;
    BC--;

    var flags = (Flags)((byte)_flags & 0b_1110_1001);
    if (BC != 0)
      flags |= Flags.Parity; 
    
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void LDIR()
  {
    LDI();
    if (ParityFlag)
      _pc -= 2;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void NEG()
  {
    var difference = 0 - _a;
    
    var flags = (Flags)(difference & 0b_1010_1000) | Flags.Negative;
    if ((byte)difference == 0)
      flags |= Flags.Zero;
    if (_a.LowNibble() > 0)
      flags |= Flags.Halfcarry;
    if (_a == 0x80)
      flags |= Flags.Parity;
    if (_a != 0)
      flags |= Flags.Carry;

    _a = (byte)difference;
    _flags = flags;   
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void OR()
  {
    var result = _a | ReadByteOperand(_instruction.Source);

    var flags = (Flags)(result & 0b_1010_1000);
    if ((byte)result == 0)
      flags |= Flags.Zero;

    if (CheckParity((byte)result))
      flags |= Flags.Parity;

    _a = (byte)result;
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void OUT()
  {
    var port = ReadByteOperand(_instruction.Destination);
    var value = _instruction.Source == Operand.Implied
              ? (byte)0x00
              : ReadByteOperand(_instruction.Source);

    WritePort(port, value);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void OUTD()
  {
    var value = _bus.ReadByte(HL);
    WritePort(_c, value);

    HL--;
    _b--;
    
    var flags = (Flags)(_b & 0b_1010_1000) | Flags.Negative;
    if (_b == 0)
      flags |= Flags.Zero;
    if (CarryFlag)
      flags |= Flags.Carry;

    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void OTDR()
  {
    OUTD();
    if (!ZeroFlag)
      _pc -= 2;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void OUTI()
  {
    var value = _bus.ReadByte(HL);
    WritePort(_c, value);

    HL++;
    _b--;
    
    var flags = (Flags)(_b & 0b_1010_1000) | Flags.Negative;
    if (_b == 0)
      flags |= Flags.Zero;
    if (CarryFlag)
      flags |= Flags.Carry;

    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void OTIR()
  {
    OUTI();
    if (!ZeroFlag)
      _pc -= 2;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void POP()
  {
    var value = PopFromStack();
    WriteWordResult(value);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void PUSH()
  {
    var value = ReadWordOperand(_instruction.Source);
    PushToStack(value);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RES(byte index)
  {
    var value = ReadByteOperand(_instruction.Destination).ResetBit(index);
    WriteByteResult(value);

    if (_instruction.Source != Operand.Implied)
      WriteByteResult(value, _instruction.Source);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RET()
  {
    if (EvaluateCondition())
      _pc = PopFromStack();
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RETI()
  {
    _pc = PopFromStack();
    _iff1 = _iff2;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RETN()
  {
    _pc = PopFromStack();
    _iff1 = _iff2;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RL()
  {
    var value = ReadByteOperand(_instruction.Destination);
    var msb = value.MSB();
    value = (byte)(value << 1);
    if (CarryFlag) value++;

    var flags = (Flags)(value & 0b_1010_1000);
    if (value == 0)
      flags |= Flags.Zero;
    if (CheckParity(value))
      flags |= Flags.Parity;
    if (msb)
      flags |= Flags.Carry;

    WriteByteResult(value);
    _flags = flags;  

    if (_instruction.Source != Operand.Implied)
      WriteByteResult(value, _instruction.Source);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RLA()
  {
    var value = (byte)(_a << 1);
    if (CarryFlag) value++;

    var flags = (Flags)(0b_1100_0100 & (byte)_flags) | 
                (Flags)(0b_0010_1000 & value);
    if (_a.MSB())
      flags |= Flags.Carry;

    _a = value;
    _flags = flags;  
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RLC()
  {
    var value = ReadByteOperand(_instruction.Destination);
    var msb = value.MSB();
    value = (byte)(value << 1);
    if (msb) value++;
    
    var flags = (Flags)(value & 0b_1010_1000);
    if (value == 0)
      flags |= Flags.Zero;
    if (CheckParity(value))
      flags |= Flags.Parity;
    if (msb)
      flags |= Flags.Carry;

    WriteByteResult(value);
    _flags = flags;  

    if (_instruction.Source != Operand.Implied)
      WriteByteResult(value, _instruction.Source);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RLCA()
  {
    var value = (byte)(_a << 1);
    var msb = _a.MSB();
    if (msb) value++;

    var flags = (Flags)(0b_1100_0100 & (byte)_flags) |
                (Flags)(0b_0010_1000 & value);
    if (msb)
      flags |= Flags.Carry;

    _a = value;
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RLD()
  {
    var address = HL;
    var value = _bus.ReadByte(address);
    var highNibble = value.HighNibble();

    value = (byte)((value.LowNibble() << 4) + _a.LowNibble());
    _a = (byte)((_a & 0b_1111_0000) + highNibble);

    var flags = (Flags)(_a & 0b_1000_0000);
    if (_a == 0)
      flags |= Flags.Zero;
    if (CheckParity(_a))
      flags |= Flags.Parity;
    if (CarryFlag)
      flags |= Flags.Carry;

    _bus.WriteByte(address, value);
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RR()
  {
    var value = ReadByteOperand(_instruction.Destination);
    var lsb = value.LSB();
    value = (byte)(value >> 1);

    if (CarryFlag)
      value |= 0b_1000_0000;

    var flags = (Flags)(value & 0b_1010_1000);
    if (value == 0)
      flags |= Flags.Zero;
    if (CheckParity(value))
      flags |= Flags.Parity;
    if (lsb)
      flags |= Flags.Carry;

    WriteByteResult(value);
    _flags = flags;  
    
    if (_instruction.Source != Operand.Implied)
      WriteByteResult(value, _instruction.Source);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RRA()
  {
    var lsb = _a.LSB();
    _a = (byte)(_a >> 1);

    if (CarryFlag)
      _a |= 0b_1000_0000;
    
    var flags = (Flags)(0b_1100_0100 & (byte)_flags) | 
                (Flags)(0b_0010_1000 & _a);
    if (lsb)
      flags |= Flags.Carry;

    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RRC()
  {
    var value = ReadByteOperand(_instruction.Destination);
    var lsb = value.LSB();
    value = (byte)(value >> 1);

    if (lsb) 
      value |= 0b_1000_0000;

    var flags = (Flags)(value & 0b_1010_1000);
    if (value == 0)
      flags |= Flags.Zero;
    if (CheckParity(value))
      flags |= Flags.Parity;
    if (lsb)
      flags |= Flags.Carry;

    WriteByteResult(value);
    _flags = flags;  
    
    if (_instruction.Source != Operand.Implied)
      WriteByteResult(value, _instruction.Source);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RRCA()
  {
    var lsb = _a.LSB();
    _a = (byte)(_a >> 1);
    
    if (lsb) 
      _a |= 0b_1000_0000;
    
    var flags = (Flags)(0b_1100_0100 & (byte)_flags) | 
                (Flags)(0b_0010_1000 & _a);
    if (lsb)
      flags |= Flags.Carry;
      
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RRD()
  {
    var address = HL;
    var value = _bus.ReadByte(address);
    var lowNibble = value.LowNibble();

    value = (byte)((_a.LowNibble() << 4) + value.HighNibble());
    _a = (byte)((_a & 0b_1111_0000) + lowNibble);

    var flags = (Flags)(_a & 0b_1010_1000);
    if (_a == 0)
      flags |= Flags.Zero;
    if (CheckParity(_a))
      flags |= Flags.Parity;
    if (CarryFlag)
      flags |= Flags.Carry;
    
    _bus.WriteByte(address, value);
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RST()
  {
    PushToStack(_pc);
    _pc = (ushort)_instruction.Destination;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SBC8()
  {
    var subtrahend = ReadByteOperand(_instruction.Source);
    var borrow = CarryFlag ? 1: 0;
    var difference = (_a - subtrahend) - borrow;

    var flags = (Flags)(difference & 0b_1010_1000) | Flags.Negative;
    if ((byte)difference == 0)
      flags |= Flags.Zero;
    if (_a.LowNibble() < subtrahend.LowNibble() + borrow)
      flags |= Flags.Halfcarry;
    if (CheckByteOverflow(_a, ~subtrahend, difference))
      flags |= Flags.Parity;
    if (difference < 0)
      flags |= Flags.Carry;

    _a = (byte)difference;
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SBC16()
  {
    var subtrahend = ReadWordOperand(_instruction.Source);
    var borrow = CarryFlag ? 1 : 0;
    var difference = (HL - subtrahend) - borrow;

    var flags = (Flags)((difference >> 8) & 0b_1010_1000) | Flags.Negative;
    if ((ushort)difference == 0)
      flags |= Flags.Zero;
    if ((HL & 0xFFF) < (subtrahend & 0xFFF) + borrow)
      flags |= Flags.Halfcarry;
    if (CheckWordOverflow(HL, ~subtrahend, difference))
      flags |= Flags.Parity;
    if (difference < 0)
      flags |= Flags.Carry;

    HL = (ushort)difference;
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SCF()
  {
    _flags = (Flags)((byte)_flags & 0b_1110_1100);
    CarryFlag = true;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SET(byte index)
  {
    var value = ReadByteOperand(_instruction.Destination).SetBit(index);
    WriteByteResult(value);
    
    if (_instruction.Source != Operand.Implied)
      WriteByteResult(value, _instruction.Source);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SLA()
  {
    var value = ReadByteOperand(_instruction.Destination);
    var msb = value.MSB();
    value = (byte)(value << 1);

    var flags = (Flags)(value & 0b_1010_1000);
    if (value == 0)
      flags |= Flags.Zero;
    if (CheckParity(value))
      flags |= Flags.Parity;
    if (msb)
      flags |= Flags.Carry;

    WriteByteResult(value);
    _flags = flags;
    
    if (_instruction.Source != Operand.Implied)
      WriteByteResult(value, _instruction.Source);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SLL()
  {
    var value = ReadByteOperand(_instruction.Destination);
    var msb = value.MSB();
    value = (byte)(value << 1);
    value++;

    var flags = (Flags)(value & 0b_1010_1000);
    if (value == 0)
      flags |= Flags.Zero;
    if (CheckParity(value))
      flags |= Flags.Parity;
    if (msb)
      flags |= Flags.Carry;

    WriteByteResult(value);
    _flags = flags;
    
    if (_instruction.Source != Operand.Implied)
      WriteByteResult(value, _instruction.Source);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SRA()
  {
    var value = ReadByteOperand(_instruction.Destination);
    var lsb = value.LSB();
    var msb = value.MSB();
    value = (byte)(value >> 1);

    if (msb)
      value |= 0b_1000_0000;

    var flags = (Flags)(value & 0b_1010_1000);
    if (value == 0)
      flags |= Flags.Zero;
    if (CheckParity(value))
      flags |= Flags.Parity;
    if (lsb)
      flags |= Flags.Carry;

    WriteByteResult(value);
    _flags = flags;
    
    if (_instruction.Source != Operand.Implied)
      WriteByteResult(value, _instruction.Source);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SRL()
  {
    var value = ReadByteOperand(_instruction.Destination);
    var lsb = value.LSB();
    value = (byte)(value >> 1);

    var flags = (Flags)(value & 0b_0010_1000);
    if (value == 0)
      flags |= Flags.Zero;
    if (CheckParity(value))
      flags |= Flags.Parity;
    if (lsb)
      flags |= Flags.Carry;

    WriteByteResult(value);
    _flags = flags;
    
    if (_instruction.Source != Operand.Implied)
      WriteByteResult(value, _instruction.Source);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SUB()
  {
    var subtrahend = ReadByteOperand(_instruction.Source);
    var difference = _a - subtrahend;

    var flags = (Flags)(difference & 0b_1010_1000) | Flags.Negative;
    if ((byte)difference == 0)
      flags |= Flags.Zero;
    if (_a.LowNibble() < subtrahend.LowNibble())
      flags |= Flags.Halfcarry;
    if (CheckByteOverflow(_a, ~subtrahend, difference))
      flags |= Flags.Parity;
    if (difference < 0)
      flags |= Flags.Carry;

    _a = (byte)difference;
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void XOR()
  {
    var result = _a ^ ReadByteOperand(_instruction.Source);
    
    var flags = (Flags)(result & 0b_1010_1000);
    if ((byte)result == 0)
      flags |= Flags.Zero;
    if (CheckParity((byte)result))
      flags |= Flags.Parity;

    _a = (byte)result;
    _flags = flags;
  }
}