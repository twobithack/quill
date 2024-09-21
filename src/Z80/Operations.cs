using Quill.Definitions;
using Quill.Extensions;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Quill
{
  public unsafe sealed partial class CPU
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ADC()
    {
      if (_instruction.Destination == Operand.HL)
      {
        var augend = _hl;
        if (_carry) augend++;

        var addend = ReadWordOperand(_instruction.Source);
        var sum = augend + addend;

        _sign = (sum & 0x8000) > 0;
        _zero = (sum == 0);
        _halfcarry = (augend & 0x0FFF) + (addend & 0x0FFF) > 0x0FFF;
        _overflow = (augend < 0x8000 && addend < 0x8000 && _sign) ||
                    (augend >= 0x8000 && addend >= 0x8000 && !_sign);
        _negative = false;
        _carry = (sum > ushort.MaxValue);

        _hl = (ushort)sum;
      }
      else
      {
        var augend = _a;
        if (_carry) augend++;

        var addend = ReadByteOperand(_instruction.Source);
        var sum = augend + addend;
        
        _sign = (sum & 0x80) > 0;
        _zero = (sum == 0);
        _halfcarry = (augend & 0x0F) + (addend & 0x0F) > 0x0F;
        _overflow = (augend < 0x80 && addend < 0x80 && _sign) ||
                    (augend >= 0x80 && addend >= 0x80 && !_sign);
        _negative = false;
        _carry = (sum > byte.MaxValue);

        _a = (byte)sum;
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ADD()
    {
      if (_instruction.IsWordOperation())
      {
        var augend = ReadWordOperand(_instruction.Destination);
        var addend = ReadWordOperand(_instruction.Source);
        var sum = augend + addend;

        _sign = (sum & 0x8000) > 0;
        _zero = (sum == 0);
        _halfcarry = (augend & 0x0FFF) + (addend & 0x0FFF) > 0x0FFF;
        _overflow = (augend < 0x8000 && addend < 0x8000 && _sign) ||
                    (augend >= 0x8000 && addend >= 0x8000 && !_sign);
        _negative = false;
        _carry = (sum > ushort.MaxValue);

        WriteWordResult((ushort)sum);
      }
      else
      {
        var augend = _a;
        var addend = ReadByteOperand(_instruction.Source);
        var sum = augend + addend;

        _sign = (sum & 0x80) > 0;
        _zero = (sum == 0);
        _halfcarry = (augend & 0x0F) + (addend & 0x0F) > 0x0F;
        _overflow = (augend < 0x80 && addend < 0x80 && _sign) ||
                    (augend >= 0x80 && addend >= 0x80 && !_sign);
        _negative = false;
        _carry = (sum > byte.MaxValue);

        _a = (byte)sum;
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AND()
    {
      var result = _a & ReadByteOperand(_instruction.Source);

      _sign = (result & 0x80) > 0;
      _zero = (result == 0);
      _halfcarry = true;
      _parity = BitOperations.PopCount((byte)result) % 2 == 0;
      _negative = false;
      _carry = false;

      _a = (byte)result;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BIT()
    {
      var value = ReadByteOperand(_instruction.Source);
      var index = GetBitIndex();

      _zero = value.TestBit(index);
      _halfcarry = true;
      _negative = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CALL()
    {
      _memPtr = FetchWord();

      if (!EvaluationCondition())
        return;

      _sp -= 2;
      _memory.WriteWord(_sp, _pc);
      _pc = _memPtr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CCF()
    {
      _halfcarry = _carry;
      _carry = !_carry;
      _negative = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CP()
    {
      var minuend = _a;
      var subtrahend = ReadByteOperand(_instruction.Source);
      var difference = minuend - subtrahend;

      _sign = (difference & 0x80) > 0;
      _zero = (minuend == subtrahend);
      _halfcarry = (minuend & 0x0F) - (subtrahend & 0x0F) < 0;
      _overflow = (minuend < 0x80 && subtrahend >= 0x80 && _sign) ||
                  (minuend >= 0x80 && subtrahend < 0x80 && !_sign);
      _negative = true;
      _carry = (subtrahend > minuend);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CPD()
    {
      var minuend = _a;
      var subtrahend = _memory.ReadByte(_hl);
      var difference = minuend - subtrahend;

      _hl++;
      _bc--;

      _sign = (difference & 0x80) > 0;
      _zero = (minuend == subtrahend);
      _halfcarry = (minuend & 0x0F) - (subtrahend & 0x0F) < 0;
      _overflow = (_bc != 0);
      _negative = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CPDR()
    {
      CPD();
      if (_overflow && !_zero)
        _pc -= 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CPI()
    {
      var minuend = _a;
      var subtrahend = _memory.ReadByte(_hl);
      var difference = minuend - subtrahend;

      _hl--;
      _bc--;

      _sign = (difference & 0x80) > 0;
      _zero = (minuend == subtrahend);
      _halfcarry = (minuend & 0x0F) - (subtrahend & 0x0F) < 0;
      _overflow = (_bc != 0);
      _negative = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CPIR()
    {
      CPI();
      if (_overflow && !_zero)
        _pc -= 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CPL()
    {
      _halfcarry = true;
      _negative = true;
      _a = (byte)~_a;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DAA()
    {
      var value = _a;

      if (_halfcarry || (_a & 0x0f) > 9)
        value = _negative
              ? (byte)(value - 0x06)
              : (byte)(value + 0x06);

      if (_carry || (_a > 0x99))
        value = _negative
              ? (byte)(value - 0x60)
              : (byte)(value + 0x60);

      _sign = value.GetMSB();
      _zero = (value == 0x00);
      _halfcarry = _a.TestBit(4) ^ value.TestBit(4);
      _parity = BitOperations.PopCount(value) % 2 == 0;
      _carry |= (_a > 0x99);

      _a = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DEC()
    {
      if (_instruction.IsWordOperation())
      {
        var minuend = ReadWordOperand(_instruction.Destination);
        var difference = minuend.Decrement();
        
        WriteWordResult(difference);
      }
      else
      {
        var minuend = ReadByteOperand(_instruction.Destination);
        var difference = minuend.Decrement();

        _sign = (difference & 0x80) > 0;
        _zero = (difference == 0);
        _halfcarry = (minuend & 0x0F) == 0;
        _overflow = (minuend >= 0x80 && !_sign);
        _negative = true;

        WriteByteResult(difference);
      }
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
      var displacement = FetchByte();
      _b--;

      if (_b != 0)
        _pc += displacement;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EI()
    {
      _iff1 = true;
      _iff2 = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EX()
    {
      ushort temp;
      switch (_instruction.Destination)
      {
        case Operand.AF: 
          temp = _af;
          _af = _afShadow;
          _afShadow = temp;
          return;

        case Operand.DE: 
          temp = _de;
          _de = _hl;
          _hl = temp;
          return;

        case Operand.SP:
          temp = _memory.ReadWord(_sp);
          switch (_instruction.Source)
          {
            case Operand.HL:
              _memory.WriteWord(_sp, _hl);
              _hl = temp;
              return;

            case Operand.IX:
              _memory.WriteWord(_sp, _ix);
              _ix = temp;
              return;

            case Operand.IY:
              _memory.WriteWord(_sp, _iy);
              _iy = temp;
              return;
          }
          return;
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EXX()
    {
      ushort temp;

      temp = _bc;
      _bc = _bcShadow;
      _bcShadow = temp;

      temp = _de;
      _de = _deShadow;
      _deShadow = temp;

      temp = _hl;
      _hl = _hlShadow;
      _hlShadow = temp;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HALT()
    {
      _halt = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void IM()
    {
      // Not used in Master System
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void IN()
    {
      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void INC()
    {
      if (_instruction.IsWordOperation())
      {
        var augend = ReadWordOperand(_instruction.Destination);
        var sum = augend.Increment();

        WriteWordResult(sum);
      }
      else
      {
        var augend = ReadByteOperand(_instruction.Destination);
        var sum = augend.Increment();
        
        _sign = (sum & 0x80) > 0;
        _zero = (sum == 0);
        _halfcarry = (augend & 0x0F) == 0x0F;
        _overflow = (augend < 0x80 && _sign);
        _negative = false;

        WriteByteResult(sum);
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void IND()
    {
      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void INI()
    {
      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void INIR()
    {
      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void JP()
    {
      _memPtr = ReadWordOperand(_instruction.Source);

      if (!EvaluationCondition())
        return;

      _pc = _memPtr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void JR()
    {
      var displacement = FetchByte();

      if (!EvaluationCondition())
        return;

      _pc += displacement;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LD()
    {
      if (_instruction.IsWordOperation())
      {
        WriteWordResult(ReadWordOperand(_instruction.Source));
      }
      else
      {
        WriteByteResult(ReadByteOperand(_instruction.Source));
      }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LDD()
    {
      var word = _memory.ReadByte(_hl);
      _memory.WriteByte(_de, word);
      
      _de--;
      _hl--;
      _bc--;

      _halfcarry = false;
      _overflow = (_bc != 0);
      _negative = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LDDR()
    {
      LDD();
      if (_overflow)
        _pc -= 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LDI()
    {
      var word = _memory.ReadByte(_hl);
      _memory.WriteByte(_de, word);
      
      _de++;
      _hl++;
      _bc--;

      _halfcarry = false;
      _overflow = (_bc != 0);
      _negative = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LDIR()
    {
      LDI();
      if (_overflow)
        _pc -= 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void NEG()
    {
      var subtrahend = ReadByteOperand(_instruction.Source);
      var difference = 0x00 - subtrahend;

      _sign = (difference & 0x80) > 0;
      _zero = (subtrahend == 0);
      _halfcarry = (subtrahend & 0x0F) > 0;
      _overflow = (subtrahend >= 0x80 && _sign);
      _negative = true;
      _carry = (subtrahend > 0);

      _a = (byte)difference;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void NOP()
    {
      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OR()
    {
      var result = _a | ReadByteOperand(_instruction.Source);

      _sign = (result & 0x80) > 0;
      _zero = (result == 0);
      _halfcarry = false;
      _parity = BitOperations.PopCount((byte)result) % 2 == 0;
      _negative = false;
      _carry = false;

      _a = (byte)result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OTDR()
    {
      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OTIR()
    {
      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OUT()
    {
      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OUTD()
    {
      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OUTI()
    {
      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void POP()
    {
      _sp -= 2;
      var word = _memory.ReadWord(_sp);
      WriteWordResult(word);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PUSH()
    {
      var word = ReadWordOperand(_instruction.Destination);
      _memory.WriteWord(_sp, word);
      _sp += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RES()
    {
      var value = ReadByteOperand(_instruction.Destination);
      var index = GetBitIndex();
      WriteByteResult(value.ResetBit(index));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RET()
    {
      if (!EvaluationCondition())
        return;

      _pc = _memory.ReadWord(_sp);
      _sp += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RETI()
    {
      _pc = _memory.ReadWord(_sp);
      _sp += 2;
      _iff1 = _iff2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RETN()
    {
      _pc = _memory.ReadWord(_sp);
      _sp += 2;
      _iff1 = _iff2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RL()
    {
      var value = ReadByteOperand(_instruction.Destination);
      var msb = value.GetMSB();
      value = (byte)(value << 1);

      if (_carry)
        value |= 1;

      _sign = value.GetMSB();
      _zero = (value == 0);
      _halfcarry = false;
      _parity = BitOperations.PopCount(value) % 2 == 0;
      _negative = false;
      _carry = msb;

      WriteByteResult(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RLA()
    {
      var value = (byte)(_a << 1);
      var msb = _a.GetMSB();

      if (_carry)
        value |= 1;

      _sign = value.GetMSB();
      _zero = (value == 0);
      _halfcarry = false;
      _parity = BitOperations.PopCount(value) % 2 == 0;
      _negative = false;
      _carry = msb;

      _a = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RLC()
    {
      var value = ReadByteOperand(_instruction.Destination);
      var msb = value.GetMSB();
      value = (byte)(value << 1);

      if (msb)
        value |= 0b_0000_0001;

      _sign = value.GetMSB();
      _zero = (value == 0);
      _halfcarry = false;
      _parity = BitOperations.PopCount(value) % 2 == 0;
      _negative = false;
      _carry = msb;

      WriteByteResult(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RLCA()
    {
      var value = (byte)(_a << 1);
      var msb = _a.GetMSB();

      if (msb)
        value |= 1;

      _sign = value.GetMSB();
      _zero = (value == 0);
      _halfcarry = false;
      _parity = BitOperations.PopCount(value) % 2 == 0;
      _negative = false;
      _carry = msb;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RLD()
    {
      _memPtr = _hl;
      var value = _memory.ReadByte(_memPtr);
      var lowNibble = _a.GetLowNibble();
      var highNibble = value.GetLowNibble();

      _a = (byte)((_a & 0b_1111_0000) + value.GetLowNibble());
      value = (byte)((highNibble << 4) + lowNibble);

      _sign = _a.GetMSB();
      _zero = (_a == 0);
      _halfcarry = false;
      _parity = BitOperations.PopCount(value) % 2 == 0;
      _negative = false;
      
      _memory.WriteByte(_memPtr, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RR()
    {
      var value = ReadByteOperand(_instruction.Destination);
      var lsb = value.GetLSB();
      value = (byte)(value >> 1);

      if (_carry)
        value |= 0b_1000_0000;

      _sign = value.GetMSB();
      _zero = (value == 0);
      _halfcarry = false;
      _parity = BitOperations.PopCount(value) % 2 == 0;
      _negative = false;
      _carry = lsb;

      WriteByteResult(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RRA()
    {
      var lsb = _a.GetLSB();
      _a = (byte)(_a >> 1);

      if (_carry)
        _a |= 0b_1000_0000;

      _sign = _a.GetMSB();
      _zero = (_a == 0);
      _halfcarry = false;
      _parity = BitOperations.PopCount(_a) % 2 == 0;
      _negative = false;
      _carry = lsb;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RRC()
    {
      var value = ReadByteOperand(_instruction.Destination);
      var lsb = _a.GetLSB();
      value = (byte)(value >> 1);
      
      if (lsb)
        value |= 0b_1000_0000;

      _sign = value.GetMSB();
      _zero = (value == 0);
      _halfcarry = false;
      _parity = BitOperations.PopCount(value) % 2 == 0;
      _negative = false;
      _carry = lsb;

      WriteByteResult(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RRCA()
    {
      var lsb = _a.GetLSB();
      _a = (byte)(_a >> 1);

      if (lsb)
        _a |= 0b_1000_0000;

      _sign = _a.GetMSB();
      _zero = (_a == 0x00);
      _halfcarry = false;
      _parity = BitOperations.PopCount(_a) % 2 == 0;
      _negative = false;
      _carry = lsb;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RRD()
    {
      _memPtr = _hl;
      var value = _memory.ReadByte(_memPtr);
      var lowNibble = value.GetHighNibble();
      var highNibble = _a.GetLowNibble();

      _a = (byte)((_a & 0b_1111_0000) + value.GetLowNibble());
      value = (byte)((highNibble << 4) + lowNibble);

      _sign = _a.GetMSB();
      _zero = (_a == 0);
      _halfcarry = false;
      _parity = BitOperations.PopCount(value) % 2 == 0;
      _negative = false;
      
      _memory.WriteByte(_memPtr, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RST()
    {
      _sp -= 2;
      _memory.WriteWord(_sp, _pc);

      _pc = _instruction.Destination switch
      {
        Operand.RST0 => 0x00,
        Operand.RST1 => 0x08,
        Operand.RST2 => 0x10,
        Operand.RST3 => 0x18,
        Operand.RST4 => 0x20,
        Operand.RST5 => 0x28,
        Operand.RST6 => 0x30,
        Operand.RST7 => 0x38
      };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SBC()
    {
      if (_instruction.Destination == Operand.HL)
      {
        var minuend = _hl;
        var subtrahend = ReadWordOperand(_instruction.Source);

        if (_carry) subtrahend++;
        var difference = minuend - subtrahend;

        _sign = (difference & 0x8000) > 0;
        _zero = (minuend == subtrahend);
        _halfcarry = (minuend & 0x0FFF) - (subtrahend & 0x0FFF) < 0;
        _overflow = (minuend < 0x8000 && subtrahend >= 0x8000 && _sign) ||
                    (minuend >= 0x8000 && subtrahend < 0x8000 && !_sign);
        _negative = true;
        _carry = (subtrahend > minuend);

        _hl = (ushort)difference;
      }
      else
      {
        var minuend = _a;
        var subtrahend = ReadByteOperand(_instruction.Source);

        if (_carry) subtrahend++;
        var difference = minuend - subtrahend;

        _sign = (difference & 0x80) > 0;
        _zero = (minuend == subtrahend);
        _halfcarry = (minuend & 0x0F) - (subtrahend & 0x0F) < 0;
        _overflow = (minuend < 0x80 && subtrahend >= 0x80 && _sign) ||
                    (minuend >= 0x80 && subtrahend < 0x80 && !_sign);
        _negative = true;
        _carry = (subtrahend > minuend);

        _a = (byte)difference;
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SCF()
    {
      _carry = true;
      _halfcarry = false;
      _negative = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SET()
    {
      var value = ReadByteOperand(_instruction.Destination);
      var index = GetBitIndex();
      WriteByteResult(value.SetBit(index));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SLA()
    {
      var value = ReadByteOperand(_instruction.Destination);
      var msb = value.GetMSB();
      value = (byte)(value << 1);

      _sign = value.GetMSB();
      _zero = (value == 0);
      _halfcarry = false;
      _parity = BitOperations.PopCount(value) % 2 == 0;
      _negative = false;
      _carry = msb;

      WriteByteResult(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SRA()
    {
      var value = ReadByteOperand(_instruction.Destination);
      var lsb = value.GetLSB();
      var msb = value.GetMSB();
      value = (byte)(value >> 1);

      if (msb)
        value |= 0b_1000_0000;

      _sign = msb;
      _zero = (value == 0);
      _halfcarry = false;
      _parity = BitOperations.PopCount(value) % 2 == 0;
      _negative = false;
      _carry = lsb;

      WriteByteResult(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SRL()
    {
      var value = ReadByteOperand(_instruction.Destination);
      var lsb = value.GetLSB();
      value = (byte)(value >> 1);

      _sign = false;
      _zero = (value == 0);
      _halfcarry = false;
      _parity = BitOperations.PopCount(value) % 2 == 0;
      _negative = false;
      _carry = lsb;

      WriteByteResult(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SUB()
    {
      var minuend = _a;
      var subtrahend = ReadByteOperand(_instruction.Source);
      var difference = minuend - subtrahend;

      _sign = (difference & 0x80) > 0;
      _zero = (minuend == subtrahend);
      _halfcarry = (minuend & 0x0F) - (subtrahend & 0x0F) < 0;
      _overflow = (minuend < 0x80 && subtrahend >= 0x80 && _sign) ||
                  (minuend >= 0x80 && subtrahend < 0x80 && !_sign);
      _negative = true;
      _carry = (subtrahend > minuend);

      _a = (byte)difference;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void XOR()
    {
      var result = _a ^ ReadByteOperand(_instruction.Source);

      _sign = (result & 0x80) > 0;
      _zero = (result == 0);
      _halfcarry = false;
      _parity = BitOperations.PopCount((byte)result) % 2 == 0;
      _negative = false;
      _carry = false;

      _a = (byte)result;
    }
  }
}