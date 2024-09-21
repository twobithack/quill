using Quill.CPU;
using Quill.Input;
using Quill.Video;
using System.Diagnostics;
using System.IO;

namespace Quill;

unsafe public class Emulator
{
  private const double FRAME_TIME_MS = 1000d / 60d;
  private const double SYSTEM_CYCLES_PER_FRAME = 10738580d / 60d; 

  public Joypads Input;
  private VDP _vdp;
  private byte[] _rom; 

  public Emulator(string romPath)
  {
    Input = new Joypads();
    _vdp = new VDP();
    _rom = ReadROM(romPath);
  }

  public void Run()
  {
    var cpu = new Z80(_rom, _vdp, Input);
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
        var vdpCycles = (double)systemCycles / 2d;
        
        _vdp.Update(vdpCycles);

        cyclesThisFrame += systemCycles;
      }

      lastFrameTime = currentTime;
    }
  }

  public byte[] ReadFramebuffer() => _vdp.ReadFramebuffer();
  private static byte[] ReadROM(string path) => File.ReadAllBytes(path);
}