using Quill.CPU;
using Quill.Input;
using Quill.Video;
using System.Diagnostics;

namespace Quill;

unsafe public class Emulator
{
  #region Constants
  private const double FRAME_TIME_MS = 1000d / 60d;
  private const int CYCLES_PER_SCANLINE = 228;
  #endregion

  #region Fields
  private readonly byte[] _rom;
  private readonly IO _io;
  private readonly VDP _vdp;
  #endregion

  public Emulator(byte[] rom, bool fixSlowdown)
  {
    _io = new IO();
    _vdp = new VDP(fixSlowdown);
    _rom = rom;
  }

  #region Methods
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

      var scanlines = _vdp.ScanlinesPerFrame;
      while (scanlines > 0)
      {
        cpu.RunFor(CYCLES_PER_SCANLINE);
        _vdp.RenderScanline();
        scanlines--;
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
  #endregion
}