using Sonic.Definitions;
using Sonic.Extensions;
using static Sonic.Definitions.Opcodes;

namespace Sonic
{
  public partial class CPU
  {
    private Memory _memory;
    private int _cycleCount;
    private int _instructionCount;

    
    public CPU()
    {
      _memory = new Memory();
    }
    
    public void Step()
    {
      FetchInstruction();
      ExecuteInstruction();

      _instructionCount++;
    }
    
    private Instruction _cir = new Instruction();
    private ushort _addressBus = 0x00;

    private byte FetchByte() => _memory[PC++];

    private ushort FetchWord()
    {
      var low = FetchByte();
      var high = FetchByte();
      return Util.ConcatBytes(high, low);
    }

    private void FetchInstruction()
    {
      var opcode = new byte[3];
      opcode[0] = FetchByte();

      if (!Opcodes.IsPrefix(opcode[0]))
      {
        _cir = Decode(opcode);
        return;
      }

      opcode[1] = FetchByte();
      
      if (opcode[1] == 0xCB &&
         (opcode[0] == 0xDD || opcode[1] == 0xFD))
      {
        opcode[2] = FetchByte();
      }
 
        _cir = Decode(opcode);
    }

    private void ExecuteInstruction()
    {
      switch (_cir.Operation)
      {
        case Operation.ADC:   ADC();  break;
        case Operation.ADD:   ADD();  break;
        case Operation.AND:   AND();  break;
        case Operation.BIT:   BIT();  break;
        case Operation.CALL:  CALL(); break;
        case Operation.CCF:   CCF();  break;
        case Operation.CP:    CP();   break;
        case Operation.CPD:   CPD();  break;
        case Operation.CPI:   CPI();  break;
        case Operation.CPIR:  CPIR(); break;
        case Operation.CPL:   CPL();  break;
        case Operation.DAA:   DAA();  break;
        case Operation.DEC:   DEC();  break;
        case Operation.DI:    DI();   break;
        case Operation.DJNZ:  DJNZ(); break;
        case Operation.EI:    EI();   break;
        case Operation.EX:    EX();   break;
        case Operation.EXX:   EXX();  break;
        case Operation.HALT:  HALT(); break;
        case Operation.IM:    IM();   break;
        case Operation.IN:    IN();   break;
        case Operation.INC:   INC();  break;
        case Operation.IND:   IND();  break;
        case Operation.INI:   INI();  break;
        case Operation.INIR:  INIR(); break;
        case Operation.JP:    JP();   break;
        case Operation.JR:    JR();   break;
        case Operation.LD:    LD();   break;
        case Operation.NEG:   NEG();  break;
        case Operation.NOP:   NOP();  break;
        case Operation.OR:    OR();   break;
        case Operation.OTDR:  OTDR(); break;
        case Operation.OTIR:  OTIR(); break;
        case Operation.OUT:   OUT();  break;
        case Operation.OUTD:  OUTD(); break;
        case Operation.OUTI:  OUTI(); break;
        case Operation.POP:   POP();  break;
        case Operation.PUSH:  PUSH(); break;
        case Operation.RES:   RES();  break;
        case Operation.RL:    RL();   break;
        case Operation.RLA:   RLA();  break;
        case Operation.RLC:   RLC();  break;
        case Operation.RLCA:  RLCA(); break;
        case Operation.RLD:   RLD();  break;
        case Operation.RR:    RR();   break;
        case Operation.RRA:   RRA();  break;
        case Operation.RRC:   RRC();  break;
        case Operation.RRCA:  RRCA(); break;
        case Operation.RRD:   RRD();  break;
        case Operation.RST:   RST();  break;
        case Operation.SBC:   SBC();  break;
        case Operation.SCF:   SCF();  break;
        case Operation.SET:   SET();  break;
        case Operation.SLA:   SLA();  break;
        case Operation.SRA:   SRA();  break;
        case Operation.SRL:   SRL();  break;
        case Operation.SUB:   SUB();  break;
        case Operation.XOR:   XOR();  break;
      }
    }

    public override String ToString()
    {
      return  $"╒══════════╤════════════╤════════════╤════════════╤════════════╕\r\n" +
              $"│Registers │  AF: {AF.ToHex()} │  BC: {BC.ToHex()} │  DE: {DE.ToHex()} │  HL: {HL.ToHex()} │\r\n" +
      //        $"│          │ AF': {AFp.ToHex()} │ BC': {BCp.ToHex()} │ DE': {DEp.ToHex()} │ HL': {HLp.ToHex()} │\r\n" +
              $"│          │  IX: {IX.ToHex()} │  IY: {IY.ToHex()} │  PC: {PC.ToHex()} │  SP: {SP.ToHex()} │\r\n" +
              $"╘══════════╧════════════╧════════════╧════════════╧════════════╛\r\n" +
              $"Flags: {_flags.ToString()}, Instruction Count: {_instructionCount} ";
    }
  }
}