using Sonic.Definitions;
using Sonic.Extensions;

namespace Sonic
{
  public class Instruction
  {
    private byte _opcode;
    private byte _prefix0;
    private byte _prefix1;

    public Instruction(byte op)
    {
      if (Opcodes.Prefixes.Contains(op))
      {
        _prefix0 = op;
      }
      else
      {
        _opcode = op;
      }
    }
    
    public void AppendByte(byte n)
    {
      // if (isDoublePrefixed)
      // {
      //   _opcode = n;
      // }
      
      if (n == 0xCB)
      {
        _prefix1 = 0xCB;
        return;
      }

      _opcode = n;
    }

    public bool isPrefixed => _prefix0 != 0x00;
    public bool isDoublePrefixed => _prefix1 == 0xCB;
    public Operation Operation => Opcodes.DecodeOpcode(_prefix0, _prefix1, _opcode);
  }
}
