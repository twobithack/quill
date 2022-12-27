global using Quill.CPU;
global using Quill.Video;
using System.Diagnostics;

namespace Quill;

public unsafe sealed class Quill
{
  public static void Main(string[] args)
  {
    var rom = ReadROM(@"test/sdsc.sms");
    var vdp = new VDP();
    var z80 = new Z80(rom, vdp);

    #if DEBUG
    z80.InitializeSDSC();
    #endif

    var instructionCount = 0ul;
    var sw = new Stopwatch();
    sw.Start();

    while (instructionCount < 100000000ul)
    {
      z80.Step();
      instructionCount++;
    }

    sw.Stop();

    Console.WriteLine(z80.ToString());
    Console.WriteLine($"{sw.ElapsedMilliseconds}ms elapsed, ({(instructionCount * 1000ul) / (ulong)(sw.ElapsedMilliseconds)} per second)");

    #if DEBUG
    z80.DumpMemory("mem.txt");
    #endif 
  }

  private static byte[] ReadROM(string path) => File.ReadAllBytes(path);
}