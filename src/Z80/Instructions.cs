using Quill.Extensions;

namespace Quill.Z80
{
  public partial class CPU
  {
    private void ADC()
    {
      if (_opcode.Destination == Operand.HL)
      {
        var a = HL;
        if (Carry) a++;

        var b = GetWordOperand(_opcode.Source);
        var result = a + b;

        SetArithmeticFlags16(result);
        Halfcarry = (a & 0x0FFF) + (b & 0x0FFF) > 0x0FFF;
        Negative = false;
        Overflow = Carry ^ (a.GetLowByte() + b.GetLowByte() > short.MaxValue);
        HL = (ushort)(result & ushort.MaxValue);
      }
      else
      {
        var a = A;
        if (Carry) a++;

        var b = GetByteOperand(_opcode.Source);
        var result = a + b;

        SetArithmeticFlags(result);
        Halfcarry = (a & 0x0F) + (b & 0x0F) > 0x0F;
        Negative = false;
        Overflow = Carry ^ ((a & sbyte.MaxValue) + (b & sbyte.MaxValue) > sbyte.MaxValue);
        A = (byte)(result & byte.MaxValue);
      }
    }

    private void ADD()
    {
      if (IsWordOperation())
      {
        var a = GetWordOperand(_opcode.Destination);
        var b = GetWordOperand(_opcode.Source);
        var result = a + b;

        SetArithmeticFlags16(result);
        Halfcarry = (a & 0x0FFF) + (b & 0x0FFF) > 0x0FFF;
        Negative = false;
        Overflow = Carry ^ ((a & short.MaxValue) + (b & short.MaxValue) > short.MaxValue);
        SetWordValue((ushort)(result & ushort.MaxValue));
      }
      else
      {
        var a = A;
        var b = GetByteOperand(_opcode.Source);
        var result = a + b;

        SetArithmeticFlags(result);
        Halfcarry = (a & 0x0F) + (b & 0x0F) > 0x0F;
        Negative = false;
        Overflow = Carry ^ (a & sbyte.MaxValue) + (b & sbyte.MaxValue) > sbyte.MaxValue;
        A = (byte)(result & byte.MaxValue);
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
      if (IsWordOperation())
      {
        SetWordValue(GetWordOperand(_opcode.Source));
      }
      else
      {
        var value = GetByteOperand(_opcode.Source);
        Console.WriteLine($"Operand value: {value}");
        SetByteValue(value);
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