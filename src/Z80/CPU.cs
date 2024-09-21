using Quill.Extensions;
using static Quill.Z80.Opcodes;

namespace Quill.Z80
{
  public partial class CPU
  {
    private Memory _mem;
    private int _cycleCount;
    private int _instructionCount;

    public CPU()
    {
      _mem = new Memory();
      _opcode = new Opcode();
    }

    public void LoadProgram(byte[] rom)
    {
      ushort i = 0x00;
      foreach(byte b in rom) 
        _mem[i++] = b;
    }
    
    public void Step()
    {
      FetchInstruction();
      ExecuteInstruction();

      _instructionCount++;
    }
    
    private Opcode _opcode;

    private ushort _address = 0x00;

    private byte FetchByte() => _mem[PC++];

    private ushort FetchWord()
    {
      var lowByte = FetchByte();
      var highByte = FetchByte();
      return highByte.Concatenate(lowByte);
    }

    private void SetArithmeticFlags(int result)
    {
        Sign = ((result >> 7) & 1) != 0;
        Zero = (result == 0);
        Carry = (result > byte.MaxValue);
    }
    
    private void SetArithmeticFlags16(int result)
    {
        Sign = ((result >> 15) & 1) != 0;
        Zero = (result == 0);
        Carry = (result > ushort.MaxValue);
    }

    public override String ToString()
    {
      return  $"╒══════════╤═══════════╤═══════════╤═══════════╤═══════════╕\r\n" +
              $"│Registers │ AF: {AF.ToHex()} │ BC: {BC.ToHex()} │ DE: {DE.ToHex()} │ HL: {HL.ToHex()} │\r\n" +
              $"│          │ IX: {IX.ToHex()} │ IY: {IY.ToHex()} │ PC: {PC.ToHex()} │ SP: {SP.ToHex()} │\r\n" +
              $"╘══════════╧═══════════╧═══════════╧═══════════╧═══════════╛\r\n" +
              $"Flags: {_flags.ToString()}\r\nInstruction Count: {_instructionCount} "; 
    }
  }
}