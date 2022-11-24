using Sonic.Definitions;
using Sonic.Extensions;

namespace Sonic
{
  public class Instruction
  {
    public Operation Operation;
    public byte Opcode;
    public byte Prefix;
    public Operand Source;
    public Operand Destination;
    public ushort Parameter0;
    public ushort Parameter1;

    public Instruction(byte op)
    {
      if (_prefixes.Contains(op))
      {
        Prefix = op;
        return;
      }
      Opcode = op;
    }
    
    public bool isPrefixed => Prefix != 0x00;
    private static byte[] _prefixes = new byte[] { 0xCB, 0xDD, 0xFD, 0xED };
  }
}