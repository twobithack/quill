using System.Diagnostics;

namespace Quill
{
  public unsafe sealed class Quill
  {
    public static void Main(string[] args)
    {
      var vdp = new VDP();
      var cpu = new CPU(vdp);
      var cycles = 0ul;
      var program = ReadROM(@"test/sonic.sms");
      cpu.LoadROM(program);

      var sw = new Stopwatch();
      sw.Start();
      while (cycles < 100000000ul)
      {
#if DEBUG
        Console.WriteLine(cpu);
        Console.Read();
#endif
        cpu.Step();
        cycles++;
      }
      sw.Stop();

#if DEBUG
      cpu.DumpMemory("mem.txt");
      cpu.DumpROM("rom.txt");
#endif

      Console.WriteLine(cpu);
      Console.WriteLine($"{sw.ElapsedMilliseconds}ms elapsed, ({(cycles * 1000ul) / (ulong)(sw.ElapsedMilliseconds)} per second)");
      // Console.Read();
    }

    private static byte[] ReadROM(string path) => File.ReadAllBytes(path);
  }
}
