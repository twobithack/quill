using Quill.Extensions;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Quill.Z80
{
  public partial class CPU
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ADC()
    {
      if (_instruction.Destination == Operand.HL)
      {
        var augend = _hl;
        if (_carry) augend++;

        var addend = ReadWord(_instruction.Source);
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

        var addend = ReadByte(_instruction.Source);
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
        var augend = ReadWord(_instruction.Destination);
        var addend = ReadWord(_instruction.Source);
        var sum = augend + addend;

        _sign = (sum & 0x8000) > 0;
        _zero = (sum == 0);
        _halfcarry = (augend & 0x0FFF) + (addend & 0x0FFF) > 0x0FFF;
        _overflow = (augend < 0x8000 && addend < 0x8000 && _sign) ||
                    (augend >= 0x8000 && addend >= 0x8000 && !_sign);
        _negative = false;
        _carry = (sum > ushort.MaxValue);

        WriteWord((ushort)sum);
      }
      else
      {
        var augend = _a;
        var addend = ReadByte(_instruction.Source);
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
      var result = _a & ReadByte(_instruction.Source);

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
      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CALL()
    {
      _memPtr = FetchWord();

      if (!EvaluateCondition())
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
      var subtrahend = ReadByte(_instruction.Source);
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

      if (!_overflow || _zero)
        return;
        
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
      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DEC()
    {
      if (_instruction.IsWordOperation())
      {
        var minuend = ReadWord(_instruction.Destination);
        var difference = minuend.Decrement();
        
        WriteWord(difference);
      }
      else
      {
        var minuend = ReadByte(_instruction.Destination);
        var difference = minuend.Decrement();

        _sign = (difference & 0x80) > 0;
        _zero = (difference == 0);
        _halfcarry = (minuend & 0x0F) == 0;
        _overflow = (minuend >= 0x80 && !_sign);
        _negative = true;

        WriteByte(difference);
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
          _af = _afS;
          _afS = temp;
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
      _bc = _bcS;
      _bcS = temp;

      temp = _de;
      _de = _deS;
      _deS = temp;

      temp = _hl;
      _hl = _hlS;
      _hlS = temp;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HALT()
    {
      
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
        var augend = ReadWord(_instruction.Destination);
        var sum = augend.Increment();

        WriteWord(sum);
      }
      else
      {
        var augend = ReadByte(_instruction.Destination);
        var sum = augend.Increment();
        
        _sign = (sum & 0x80) > 0;
        _zero = (sum == 0);
        _halfcarry = (augend & 0x0F) == 0x0F;
        _overflow = (augend < 0x80 && _sign);
        _negative = false;

        WriteByte(sum);
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
      _memPtr = ReadWord(_instruction.Source);

      if (!EvaluateCondition())
        return;

      _pc = _memPtr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void JR()
    {
      var displacement = FetchByte();

      if (!EvaluateCondition())
        return;

      _pc += displacement;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LD()
    {
      if (_instruction.IsWordOperation())
      {
        WriteWord(ReadWord(_instruction.Source));
      }
      else
      {
        WriteByte(ReadByte(_instruction.Source));
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
      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void NEG()
    {
      var subtrahend = ReadByte(_instruction.Source);
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
      var result = _a | ReadByte(_instruction.Source);

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
      WriteWord(word);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PUSH()
    {
      var word = ReadWord(_instruction.Destination);
      _memory.WriteWord(_sp, word);
      _sp += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RES()
    {
      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RET()
    {
      if (!EvaluateCondition())
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
      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RLA()
    {
      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RLC()
    {
      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RLCA()
    {
      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RLD()
    {
      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RR()
    {
      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RRA()
    {
      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RRC()
    {
      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RRCA()
    {
      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RRD()
    {
      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RST()
    {
      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SBC()
    {
      var minuend = _a;
      var subtrahend = ReadByte(_instruction.Source);

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

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SLA()
    {

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SRA()
    {

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SRL()
    {

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SUB()
    {
      var minuend = _a;
      var subtrahend = ReadByte(_instruction.Source);
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
      var result = _a ^ ReadByte(_instruction.Source);

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