using Quill.Extensions;
using System.Numerics;

namespace Quill.Z80
{
  public partial class CPU
  {
    private void ADC()
    {
      if (_registers.Instruction.Destination == Operand.HL)
      {
        var a = _registers.HL;
        if (_registers.Carry) a++;

        var b = ReadWord(_registers.Instruction.Source);
        var sum = a + b;

        _registers.Sign = (sum & 0x8000) > 0;
        _registers.Zero = (sum == 0);
        _registers.Halfcarry = (a & 0x0FFF) + (b & 0x0FFF) > 0x0FFF;
        _registers.Negative = false;
        _registers.Overflow = (a < 0x8000 && b < 0x8000 && _registers.Sign) ||
                              (a >= 0x8000 && b >= 0x8000 && !_registers.Sign);
        _registers.Carry = (sum > ushort.MaxValue);

        _registers.HL = (ushort)sum;
      }
      else
      {
        var a = _registers.A;
        if (_registers.Carry) a++;

        var b = ReadByte(_registers.Instruction.Source);
        var sum = a + b;
        
        _registers.Sign = (sum & 0x80) > 0;
        _registers.Zero = (sum == 0);
        _registers.Halfcarry = (a & 0x0F) + (b & 0x0F) > 0x0F;
        _registers.Negative = false;
        _registers.Overflow = (a < 0x80 && b < 0x80 && _registers.Sign) ||
                              (a >= 0x80 && b >= 0x80 && !_registers.Sign);
        _registers.Carry = (sum > byte.MaxValue);

        _registers.A = (byte)sum;
      }
    }

    private void ADD()
    {
      if (IsWordOperation())
      {
        var a = ReadWord(_registers.Instruction.Destination);
        var b = ReadWord(_registers.Instruction.Source);
        var sum = a + b;

        _registers.Sign = (sum & 0x8000) > 0;
        _registers.Zero = (sum == 0);
        _registers.Halfcarry = (a & 0x0FFF) + (b & 0x0FFF) > 0x0FFF;
        _registers.Negative = false;
        _registers.Overflow = (a < 0x8000 && b < 0x8000 && _registers.Sign) ||
                              (a >= 0x8000 && b >= 0x8000 && !_registers.Sign);
        _registers.Carry = (sum > ushort.MaxValue);

        WriteWord((ushort)sum);
        Console.WriteLine($"{a.ToHex()} + {b.ToHex()} = {_registers.A.ToHex()}");
      }
      else
      {
        var a = _registers.A;
        var b = ReadByte(_registers.Instruction.Source);
        var sum = a + b;

        _registers.Sign = (sum & 0x80) > 0;
        _registers.Zero = (sum == 0);
        _registers.Halfcarry = (a & 0x0F) + (b & 0x0F) > 0x0F;
        _registers.Negative = false;
        _registers.Overflow = (a < 0x80 && b < 0x80 && _registers.Sign) ||
                              (a >= 0x80 && b >= 0x80 && !_registers.Sign);
        _registers.Carry = (sum > byte.MaxValue);

        _registers.A = (byte)sum;
        Console.WriteLine($"{a.ToHex()} + {b.ToHex()} = {_registers.A.ToHex()}");
      }
    }

    private void AND()
    {
      var a = _registers.A;
      var b = ReadByte(_registers.Instruction.Source);
      var result = a & b;

      _registers.Sign = (result & 0x80) > 0;
      _registers.Zero = (result == 0);
      _registers.Halfcarry = true;
      _registers.Parity = BitOperations.PopCount((byte)result) % 2 == 0;
      _registers.Negative = false;
      _registers.Carry = false;

      _registers.A = (byte)(result & byte.MaxValue);
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
      
    }

    private void DAA()
    {
      
    }

    private void DEC()
    {
      
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
      if (IsWordOperation())
      {
        WriteWord(ReadWord(_registers.Instruction.Source));
      }
      else
      {
        WriteByte(ReadByte(_registers.Instruction.Source));
      }
    }

    private void NEG()
    {
      
    }

    private void NOP()
    {
      
    }

    private void OR()
    {
      
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
      var minuend = _registers.A;
      var subtrahend = ReadByte(_registers.Instruction.Source);

      if (_registers.Carry) subtrahend++;
      var difference = minuend - subtrahend;

      _registers.Sign = (difference & 0x80) > 0;
      _registers.Zero = (difference == 0x00);
      _registers.Halfcarry = (minuend & 0x0F) - (subtrahend & 0x0F) < 0x00;
      _registers.Negative = true;
      _registers.Overflow = (minuend < 0x80 && subtrahend < 0x80 && _registers.Sign) ||
                            (minuend >= 0x80 && subtrahend >= 0x80 && !_registers.Sign);
      _registers.Carry = (subtrahend > minuend);

      _registers.A = (byte)difference;
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
      var minuend = _registers.A;
      var subtrahend = ReadByte(_registers.Instruction.Source);
      var difference = minuend - subtrahend;

      _registers.Sign = (difference & 0x80) > 0;
      _registers.Zero = (difference == 0x00);
      _registers.Halfcarry = (minuend & 0x0F) - (subtrahend & 0x0F) < 0x00;
      _registers.Negative = true;
      _registers.Overflow = (minuend < 0x80 && subtrahend < 0x80 && _registers.Sign) ||
                            (minuend >= 0x80 && subtrahend >= 0x80 && !_registers.Sign);
      _registers.Carry = (subtrahend > minuend);

      _registers.A = (byte)difference;
    }

    private void XOR()
    {

    }
  }
}