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

    private byte Fetch() => _memory[_registers.PC++];

    private Word FetchWord() => new Word(Fetch(), Fetch());
    
    public void Step()
    {
      // fetch
      var instruction = Fetch();

      // decode
      switch (instruction)
      {
        case 0x00:
          NOP();
          break;

        case 0x01:
          LD();
          break;
        
        default:
          NOP();
          break;
      }

      // execute
    }

    private void NOP() => _cycles += 4;

    private void LD()
    {
      var args = FetchWord();
      // decode src/dst...
    }

    public override string ToString() => _registers.ToString() + "\r\n" + $"Cycles: {_cycles}";
  }
}