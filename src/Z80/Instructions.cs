using Quill.Extensions;
using System.Numerics;

namespace Quill.Z80
{
  public partial class CPU
  {
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
    
    private void BIT()
    {
      
    }

    
    private void CALL()
    {
      
    }

    private void CCF()
    {

    }

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

    private void CPD()
    {
      
    }

    private void CPI()
    {
      
    }

    private void CPIR()
    {
      
    }

    private void CPL()
    {
      _halfcarry = true;
      _negative = true;
      _a = (byte)~_a;
    }

    private void DAA()
    {
      
    }

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

    private void DI()
    {
      
    }

    private void DJNZ()
    {
      
    }

    private void EI()
    {
      
    }

    private void EX()
    {
      
    }

    private void EXX()
    {
      
    }

    private void EN()
    {
      
    }
 
    private void HALT()
    {
      
    }

    private void IM()
    {
      
    }
 
    private void IN()
    {
      
    }

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

    private void IND()
    {
      
    }

    private void INI()
    {
      
    }

    private void INIR()
    {
      
    }

    private void JP()
    {
      
    }

    private void JR()
    {
      
    }

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

    private void NOP()
    {
      
    }

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

    private void OTDR()
    {
      
    }

    private void OTIR()
    {
      
    }

    private void OUT()
    {
      
    }

    private void OUTD()
    {
      
    }

    private void OUTI()
    {
      
    }

    private void POP()
    {
      
    }

    private void PUSH()
    {
      
    }

    private void RES()
    {
      
    }

    private void RL()
    {
      
    }

    private void RLA()
    {
      
    }

    private void RLC()
    {
      
    }

    private void RLCA()
    {
      
    }

    private void RLD()
    {
      
    }

    private void RR()
    {
      
    }

    private void RRA()
    {
      
    }

    private void RRC()
    {
      
    }

    private void RRCA()
    {
      
    }

    private void RRD()
    {
      
    }

    private void RST()
    {
      
    }

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

    private void SCF()
    {

    }

    private void SET()
    {

    }

    private void SLA()
    {

    }

    private void SRA()
    {

    }

    private void SRL()
    {

    }

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