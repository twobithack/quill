using Quill.CPU;
using Quill.Input;
using Quill.Video;
using System;
using System.Diagnostics;
using System.IO;

namespace Quill;

unsafe public class Emulator
{
  private const double FRAME_TIME_MS = 1000d / 60d;
  private const double SYSTEM_CYCLES_PER_FRAME = 10738580d / 60d; 

  public Joypads Input;
  private byte[] _framebuffer;
  private byte[] _rom; 

  public Emulator(string romPath)
  {
    _framebuffer = new byte[0x30000];
    _rom = ReadROM(romPath);
    Input = new Joypads();
  }

  public void Run()
  {
    var vdp = new VDP();
    var cpu = new Z80(_rom, vdp, Input);
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

      lock (_framebuffer)
        Array.Copy(vdp.ReadFramebuffer(), _framebuffer, _framebuffer.Length);
      
      lastFrameTime = currentTime;
    }
  }

  public byte[] GetFramebuffer()
  {
    lock (_framebuffer)
      return _framebuffer;
  }

  private static byte[] ReadROM(string path) => File.ReadAllBytes(path);
}