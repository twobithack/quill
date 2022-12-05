using Quill.Z80;

namespace Quill
{
  public class Quill
  {
    private static readonly byte[] ROM = new byte[] { 0x3E, 0x0F, 0x87, 0x87, 0x87, 0x87, 0x87, 0x87, 0x87 };

    public static void Main(string[] args)
    {
      var cpu = new CPU();
      var cycles = 0;

      cpu.LoadProgram(ROM);

      while (cycles < ROM.Count())
      {
        cpu.Step();
        Console.WriteLine(cpu.ToString());
        cycles++;
      }
    }
  }
}
