using Quill.CPU;
using Quill.Input;
using Quill.Video;
using System.Diagnostics;

namespace Quill;

unsafe public class Emulator
{
  private const double FRAME_TIME_MS = 1000d / 60d;
  private const double SYSTEM_CYCLES_PER_FRAME = 10738580d / 60d;

  private readonly byte[] _rom;
  private readonly Joypads _io;
  private readonly VDP _vdp;

  public Emulator(byte[] rom)
  {
    _io = new Joypads();
    _vdp = new VDP();
    _rom = rom;
  }

  public void Run()
  {
    var cpu = new Z80(_rom, _vdp, _io);
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
        
        _vdp.Step(vdpCycles);

        cyclesThisFrame += systemCycles;
      }

      lastFrameTime = currentTime;
    }
  }

  public byte[] ReadFramebuffer() => _vdp.ReadFramebuffer();

  public void SetJoypadState(int joypad,
                             bool up, 
                             bool down, 
                             bool left, 
                             bool right, 
                             bool fireA, 
                             bool fireB)
  {
    if (joypad == 0)
      _io.SetJoypad1State(up, down, left, right, fireA, fireB);
    else
      _io.SetJoypad2State(up, down, left, right, fireA, fireB);
  }
}