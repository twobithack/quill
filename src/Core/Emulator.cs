using Quill.CPU;
using Quill.Input;
using Quill.Video;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Quill;

unsafe public class Emulator
{
  #region Constants
  private const double FRAME_TIME_MS = 1000d / 60d;
  private const int CYCLES_PER_SCANLINE = 228;
  #endregion

  #region Fields
  private readonly IO _io;
  private readonly VDP _vdp;
  private readonly byte[] _rom;
  private bool _running;
  #endregion

  public Emulator(byte[] rom, bool fixSlowdown)
  {
    _io = new IO();
    _vdp = new VDP(fixSlowdown);
    _rom = rom;
    _running = false;
  }

  #region Methods
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void Run()
  {
    var cpu = new Z80(_rom, _vdp, _io);
    var clock = new Stopwatch();
    var lastFrameTime = 0d;
    clock.Start();

    _running = true;
    while (_running)
    {
      var currentTime = clock.Elapsed.TotalMilliseconds;
      if (currentTime < lastFrameTime + FRAME_TIME_MS)
        continue;

      var scanlines = _vdp.ScanlinesPerFrame;
      while (scanlines > 0)
      {
        cpu.Run(CYCLES_PER_SCANLINE);
        _vdp.RenderScanline();
        scanlines--;
      }

      lastFrameTime = currentTime;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public byte[] ReadFramebuffer() => _vdp.ReadFramebuffer();

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void Stop() => _running = false;
  #endregion
}