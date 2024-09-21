global using Quill.CPU;
global using Quill.Video;
using System.Diagnostics;

namespace Quill;

public unsafe sealed class Quill
{
  private const double FRAME_TIME_MS = 1000d / 60d;
  private const double SYSTEM_CYCLES_PER_FRAME = 10738580d / 60d; 

  public static void Main(string[] args)
  {
    var rom = ReadROM(@"test/sdsc.sms");
    var vdp = new VDP();
    var z80 = new Z80(rom, vdp);
    var clock = new Stopwatch();
    var lastFrame = 0d;

    #if DEBUG
    z80.InitializeSDSC();
    #endif

    clock.Start();
    while (true)
    {
      var currentTime = clock.Elapsed.TotalMilliseconds;
      if (currentTime < lastFrame + FRAME_TIME_MS)
        continue;

      var cyclesThisFrame = 0d;
      while (cyclesThisFrame <= SYSTEM_CYCLES_PER_FRAME)
      {
        var cpuCycles = z80.Step();
        var systemCycles = cpuCycles * 3;
        
        vdp.Update(systemCycles);

        cyclesThisFrame += systemCycles;
      }
      
      lastFrame = currentTime;
    }
  }

  private static byte[] ReadROM(string path) => File.ReadAllBytes(path);
}