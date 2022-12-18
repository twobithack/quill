using Quill.Z80;
using System.Diagnostics;

namespace Quill
{
  public unsafe sealed class Quill
  {
    public static void Main(string[] args)
    {
      var io = new Ports();
      var cpu = new CPU(io);
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
