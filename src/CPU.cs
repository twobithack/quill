using Sonic.Definitions;
using Sonic.Extensions;

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
      var opcode = Fetch();
      var instruction = Decode(opcode);
      Execute(instruction);

      _instructionCount++;
    }
    
    private byte Fetch() => _memory[PC++];
    private ushort FetchWord()
    {
      var low = Fetch();
      var high = Fetch();
      return Util.ConcatBytes(high, low);
    }

    private Instruction Decode(byte opcode)
    {
      var instruction = new Instruction(opcode);

      if (instruction.isPrefixed)
        instruction.AppendByte(Fetch());

      if (instruction.isDoublePrefixed)
        instruction.AppendByte(Fetch());

      switch (instruction.Operation)
      {
        case 0x00:
          NOP();
          break;
      }

      return instruction;
    }

    private void Execute(Instruction x) {}

    public override String ToString()
    {
      return  $"╒══════════╤════════════╤════════════╤════════════╤════════════╕\r\n" +
              $"│Registers │  AF: {AF.ToHex()} │  BC: {BC.ToHex()} │  DE: {DE.ToHex()} │  HL: {HL.ToHex()} │\r\n" +
              $"│          │ AF': {AFp.ToHex()} │ BC': {BCp.ToHex()} │ DE': {DEp.ToHex()} │ HL': {HLp.ToHex()} │\r\n" +
              $"│          │  IX: {IX.ToHex()} │  IY: {IY.ToHex()} │  PC: {PC.ToHex()} │  SP: {SP.ToHex()} │\r\n" +
              $"╘══════════╧════════════╧════════════╧════════════╧════════════╛\r\n" +
              $"Flags: {_flags.ToString()}, Instruction Count: {_instructionCount} ";
    }
  }
}