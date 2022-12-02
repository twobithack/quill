using static Quill.Definitions.Constants;
using Quill.Definitions;
using Quill.Extensions;

namespace Quill.Z80
{
  public partial class CPU
  {
    private void SetArithmeticFlags()
    {
      
    }

    private void ADC()
    {
      if (_cir.Destination == Operand.HL)
      {
        var a = _reg.HL;
        var b = GetWordOperand(_cir.Source);
        var result = a + b;

        if (_reg.Carry) result++;

        _reg.Sign = ((result >> 15) & 1) != 0;
        _reg.Zero = (result == 0);
        _reg.Halfcarry = (a & 0x0FFF) + (b & 0x0FFF) > 0x0FFF;
        _reg.Negative = false;
        _reg.Carry = (result > ushort.MaxValue);
        _reg.Overflow = _reg.Carry ^ ((a & short.MaxValue) + (b & short.MaxValue) > short.MaxValue);

        _reg.HL = (ushort)(result & ushort.MaxValue);
      }
      else
      {
        var a = _reg.A;
        var b = GetByteOperand(_cir.Source);
        var result = a + b;

        if (_reg.Carry) result++;

        _reg.Sign = ((result >> 7) & 1) != 0;
        _reg.Zero = (result == 0);
        _reg.Halfcarry = (a & 0x0F) + (b & 0x0F) > 0x0F;
        _reg.Negative = false;
        _reg.Carry = (result > byte.MaxValue);
        _reg.Overflow = _reg.Carry ^ ((a & sbyte.MaxValue) + (b & sbyte.MaxValue) > sbyte.MaxValue);

        _reg.A = (byte)(result & byte.MaxValue);
      }
    }

    private void ADD()
    {
      if (_cir.IsWordOperation())
      {
        var a = GetWordOperand(_cir.Destination);
        var b = GetWordOperand(_cir.Source);
        var result = a + b;

        _reg.Sign = (result & (1 << 15)) != 0;
        _reg.Zero = (result == 0);
        _reg.Halfcarry = (a & 0x0FFF) + (b & 0x0FFF) > 0x0FFF;
        _reg.Negative = false;
        _reg.Carry = (result > ushort.MaxValue);
        _reg.Overflow = _reg.Carry ^ 
                        (a & short.MaxValue) + (b & short.MaxValue) > short.MaxValue;

        SetWordValue((ushort)(result & ushort.MaxValue));
      }
      else
      {
        var a = _reg.A;
        var b = GetByteOperand(_cir.Source);
        var result = a + b;

        _reg.Sign = (result & (1 << 7)) != 0;
        _reg.Zero = (result == 0);
        _reg.Halfcarry = (a & 0x0F) + (b & 0x0F) > 0x0F;
        _reg.Negative = false;
        _reg.Carry = (result > byte.MaxValue);
        _reg.Overflow = _reg.Carry ^ (a & 0x7F) + (b & 0x7F) > 0x7F;
        _reg.A = (byte)(result & byte.MaxValue);

        
      }
    }

    private void AND()
    {

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

    }

    private void XOR()
    {

    }
  }
}