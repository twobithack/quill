using System.Diagnostics;

namespace Quill;

public unsafe sealed class Quill
{
  public static void Main(string[] args)
  {
    var rom = ReadROM(@"test/sonic.sms");
    var vdp = new VDP();
    var cpu = new CPU(rom, vdp);
    var cycles = 0ul;
    //cpu.DumpROM("rom.txt");

    var sw = new Stopwatch();
    sw.Start();
    while (cycles < 1000000000ul)
    {
      cpu.Step();
      cycles++;
    }
    sw.Stop();

#if DEBUG
    cpu.DumpMemory("mem.txt");
#endif
    Console.WriteLine(cpu.ToString());
    Console.WriteLine($"{sw.ElapsedMilliseconds}ms elapsed, ({(cycles * 1000ul) / (ulong)(sw.ElapsedMilliseconds)} per second)");
    // Console.Read();
  }

  private static byte[] ReadROM(string path) => File.ReadAllBytes(path);
}