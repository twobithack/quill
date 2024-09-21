global using Quill.CPU;
global using Quill.Video;
using System.Diagnostics;
using System.IO;

namespace Quill;

unsafe public class Emulator
{
  private const double FRAME_TIME_MS = 1000d / 60d;
  private const double SYSTEM_CYCLES_PER_FRAME = 10738580d / 60d; 

  public byte[] Framebuffer;
  private byte[] _rom; 

  public Emulator(string romPath)
  {
    Framebuffer = new byte[256 * 192 * 4];
    _rom = ReadROM(romPath);
  }

  public void Run()
  {
    var vdp = new VDP();
    var cpu = new Z80(_rom, vdp);
    var clock = new Stopwatch();
    var lastFrameTime = 0d;

    #if DEBUG
    cpu.InitializeSDSC();
    #endif

    clock.Start();
    while (true)
    {
      var currentTime = clock.Elapsed.TotalMilliseconds;
      if (currentTime < lastFrameTime + FRAME_TIME_MS)
        continue;

      var cyclesThisFrame = 0d;
      while (cyclesThisFrame <= SYSTEM_CYCLES_PER_FRAME)
      {
        var cpuCycles = cpu.Step();
        var systemCycles = cpuCycles * 3;
        
        vdp.Update(systemCycles);

        cyclesThisFrame += systemCycles;
      }

      lock (Framebuffer)
      {
        Framebuffer = vdp.ReadFramebuffer();
      }

      lastFrameTime = currentTime;
    }
  }

  public byte[] GetFramebuffer()
  {
    lock (Framebuffer)
    {
      return Framebuffer;
    }
  }

  private static byte[] ReadROM(string path) => File.ReadAllBytes(path);
}