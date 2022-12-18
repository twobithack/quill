using Quill.Z80;
using System.Diagnostics;

namespace Quill
{
  public class Quill
  {
    public static void Main(string[] args)
    {
      var cpu = new CPU();
      var cycles = 0ul;
      var program = ReadROM(@"test/zexdoc.sms");
      cpu.LoadROM(program);

      var sw = new Stopwatch();
      sw.Start();
      while (cycles < 10000000ul)
      {
        cpu.Step();
        cycles++;
      }
      sw.Stop();

      cpu.DumpMemory("mem.txt");

      Console.WriteLine(cpu);
      Console.WriteLine($"{sw.ElapsedMilliseconds}ms elapsed, ({(cycles * 1000ul) / (ulong)(sw.ElapsedMilliseconds)} per second)");
      Console.Read();
    }

    private static byte[] ReadROM(string path) => File.ReadAllBytes(path);
  }
}
