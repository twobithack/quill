using System.Diagnostics;

namespace Quill;

public unsafe sealed class Quill
{
  public static void Main(string[] args)
  {
    var rom = ReadROM(@"test/sdsc.sms");
    var vdp = new VDP();
    var cpu = new CPU(rom, vdp);
    cpu.InitializeSDSC();
    var instructions = 0ul;

    var sw = new Stopwatch();
    sw.Start();
    while (instructions < 10000000ul)
    {
      cpu.Step();
      instructions++;
    }
    sw.Stop();

    cpu.DumpMemory("mem.txt");
    Console.WriteLine(cpu.ToString());
    Console.WriteLine($"{sw.ElapsedMilliseconds}ms elapsed, ({(instructions * 1000ul) / (ulong)(sw.ElapsedMilliseconds)} per second)");
  }

  private static byte[] ReadROM(string path) => File.ReadAllBytes(path);
}