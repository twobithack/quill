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
    
    private Flags _flags
    {
      get => (Flags) F;
      set => F = (byte) value;
    }

    private void SetFlag(Flags flag, bool value) => _flags = value
                                                           ? _flags | flag 
                                                           : _flags & ~flag;
    
    public void Step()
    {
      var opcode = Fetch();
      Decode(opcode);
      _instructionCount++;
    }
    
    private byte Fetch() => _memory[PC++];
    private ushort FetchWord() => Util.ConcatBytes(Fetch(), Fetch());

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