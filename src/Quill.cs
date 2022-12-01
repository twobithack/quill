using Quill.Z80;

namespace Quill
{
  public class Quill
  {
    public static void Main(string[] args)
    {
      var cpu = new CPU();
      while (true)
      {
        Console.WriteLine(cpu.ToString());
        cpu.Step();
      }
    }
  }
}
