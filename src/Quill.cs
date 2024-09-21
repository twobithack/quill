using System.Diagnostics;

namespace Quill;

public unsafe sealed class Quill
{
  public static void Main(string[] args)
  {
    var rom = ReadROM(@"test/sdsc.sms");
    var vdp = new VDP();
    var cpu = new CPU(rom, vdp);

    #if DEBUG
    cpu.InitializeSDSC();
    #endif

    var instructionCount = 0ul;
    var sw = new Stopwatch();
    sw.Start();

    while (instructionCount < 10000000ul)
    {
      cpu.Step();
      instructionCount++;
    }

    sw.Stop();

    Console.WriteLine(cpu.ToString());
    Console.WriteLine($"{sw.ElapsedMilliseconds}ms elapsed, ({(instructionCount * 1000ul) / (ulong)(sw.ElapsedMilliseconds)} per second)");

    #if DEBUG
    cpu.DumpMemory("mem.txt");
    #endif 
  }

  private static byte[] ReadROM(string path) => File.ReadAllBytes(path);
}