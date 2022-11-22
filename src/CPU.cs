using Sonic.Definitions;

namespace Sonic
{
  public class CPU
  {
    private Registers _registers;
    private Memory _memory;
    private int _cycles;

    public CPU()
    {
      _registers = new Registers();
      _memory = new Memory();
    }

    private StatusFlags _flags
    {
      get => (StatusFlags) _registers.F;
      set => _registers.F = (byte) value;
    }

    private byte Fetch() => _memory[_registers.PC++]; 
    private ushort FetchWord() => Util.ConcatBytes(Fetch(), Fetch());
    
    public void Step()
    {
      // fetch
      _registers.Instruction = Fetch();
      _cycles++;
    }

    public override string ToString() => _registers.ToString() + "\r\n" +
                                         $"Flags: {_flags.ToString()}, Cycles: {_cycles} ";
  }
}