using Sonic.Definitions;
using Sonic.Extensions;
using static Sonic.Definitions.Opcodes;

namespace Sonic
{
  public partial class CPU
  {
    private void ADC()
    {
    }

    private void ADD()
    {

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

    private byte GetByteOperand(Operand operand)
    {
      switch (operand)
      {
        case Operand.Immediate:
          return FetchByte();

        case Operand.A:
        case Operand.B:
        case Operand.C:
        case Operand.D:
        case Operand.E:
        case Operand.F:
        case Operand.H:
        case Operand.L:
          return ReadRegister(operand);

        case Operand.Indirect:
          _addressBus = FetchWord();
          break;

        case Operand.BCi:
        case Operand.DEi:
        case Operand.HLi:
          _addressBus = ReadRegisterPair(operand);
          break;

        case Operand.IXd:
        case Operand.IYd:
          _addressBus = (byte)(ReadRegister(operand) + FetchByte());
          break;
        
        default:
          return A;
      }
      return _memory[_addressBus];
    }

    private ushort GetWordOperand(Operand operand)
    {
      ushort address;
      switch (operand)
      {
        case Operand.Immediate:
          return FetchWord();

        case Operand.Indirect:
          address = FetchWord();
          break;

        case Operand.AF:
        case Operand.BC: 
        case Operand.DE: 
        case Operand.HL: 
        case Operand.IX:  
        case Operand.IY: 
        case Operand.PC:
        case Operand.SP:
          return ReadRegisterPair(operand);

        default:
          return 0x00;
      }
      return _memory[address];
    }

    private void SetByteValue(byte value)
    {
      switch (_cir.Destination)
      {
        case Operand.Indirect:
          _addressBus = FetchWord();
          break;
        
        case Operand.BCi:
          _memory[BC] = value;
          return;

        case Operand.DEi:
          _memory[DE] = value;
          return;

        case Operand.HLi:
          _memory[HL] = value;
          return;

        case Operand.A:
        case Operand.B:
        case Operand.C:
        case Operand.D:
        case Operand.E:
        case Operand.F:
        case Operand.H:
        case Operand.L:
          WriteRegister(_cir.Destination, value);
          return;

        case Operand.IXd:
        case Operand.IYd:
          _addressBus = (byte)(ReadRegister(_cir.Source) + FetchByte());
          break;

        default:
          return;
      }
      _memory[_addressBus] = value;
    }

    private void SetWordValue(ushort value)
    {
      switch (_cir.Destination)
      {
        case Operand.Indirect:
          _memory[_addressBus] = value.LowByte();
          _memory[_addressBus.Increment()] = value.HighByte();
          return;

        case Operand.AF: AF = value; return;
        case Operand.BC: AF = value; return;
        case Operand.DE: AF = value; return;
        case Operand.HL: AF = value; return;
        case Operand.IX: AF = value; return;
        case Operand.IY: AF = value; return;
        case Operand.PC: AF = value; return;
        case Operand.SP: AF = value; return;
      }
    }

    private byte ReadRegister(Operand register)
    {
      switch (register)
      {
        case Operand.A: return A;
        case Operand.B: return B;
        case Operand.C: return C;
        case Operand.D: return D;
        case Operand.E: return E;
        case Operand.F: return F;
        case Operand.H: return H;
        case Operand.L: return L;
        default:        return 0x00;
      }
    }

    private ushort ReadRegisterPair(Operand register)
    {
      switch (register)
      {
        case Operand.AF: return AF;
        case Operand.BC: return BC;
        case Operand.DE: return DE;
        case Operand.HL: return HL;
        case Operand.IX: return IX;
        case Operand.IY: return IY;
        case Operand.PC: return PC;
        case Operand.SP: return SP;
        default:         return 0x00;
      }
    }

    private void WriteRegister(Operand register, byte value)
    {
      switch (register)
      {
        case Operand.A: A = value; return;
        case Operand.B: B = value; return;
        case Operand.C: C = value; return;
        case Operand.D: D = value; return;
        case Operand.E: E = value; return;
        case Operand.F: F = value; return;
        case Operand.H: H = value; return;
        case Operand.L: L = value; return;
      }
    }

    private void WriteRegisterPair(Operand register, ushort value)
    {
      switch (register)
      {
        case Operand.AF: AF = value; return;
        case Operand.BC: BC = value; return;
        case Operand.DE: DE = value; return;
        case Operand.HL: HL = value; return;
        case Operand.IX: IX = value; return;
        case Operand.IY: IY = value; return;
        case Operand.PC: PC = value; return;
        case Operand.SP: SP = value; return;
      }
    }
  }
}