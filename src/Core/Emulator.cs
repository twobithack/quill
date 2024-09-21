using Quill.CPU;
using Quill.Input;
using Quill.Video;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Quill.Core;

unsafe public class Emulator
{
  #region Constants
  private const double FRAME_TIME_MS = 1000d / 60d;
  private const int CYCLES_PER_SCANLINE = 228;
  #endregion

  #region Fields
  private readonly IO _input;
  private readonly VDP _video;
  private readonly Stopwatch _clock;
  private readonly byte[] _rom;
  private bool _running;
  private bool _loadRequested;
  private bool _saveRequested;
  private string _snapshotPath;
  #endregion

  public Emulator(byte[] rom, int fakeScanlines)
  {
    _input = new IO();
    _video = new VDP(fakeScanlines);
    _clock = new Stopwatch();
    _rom = rom;
  }

  #region Methods
  public void Run()
  {
    var cpu = new Z80(_rom, _input, _video);
    var lastFrame = 0d;

    _clock.Start();
    _running = true;
    while (_running)
    {
      var currentTime = _clock.Elapsed.TotalMilliseconds;
      if (currentTime < lastFrame + FRAME_TIME_MS)
        continue;

      lastFrame = currentTime;
      var scanlines = _video.ScanlinesPerFrame;
      while (scanlines > 0)
      {
        cpu.Run(CYCLES_PER_SCANLINE);
        _video.RenderScanline();
        scanlines--;
      }

      if (_loadRequested)
      {
        var state = LoadSnapshot();
        if (state != null)
          cpu.LoadState(state);
        _loadRequested = false;
      }
      else if (_saveRequested)
      {
        var state = cpu.SaveState();
        SaveSnapshot(state);
        _saveRequested = false;
      }
    }
  }

  public byte[] ReadFramebuffer() => _video.ReadFramebuffer();

  public void SetJoypadState(int joypad,
                             bool up, 
                             bool down, 
                             bool left, 
                             bool right, 
                             bool fireA, 
                             bool fireB,
                             bool pause)
  {
    if (joypad == 0)
      _input.SetJoypad1State(up, down, left, right, fireA, fireB, pause);
    else
      _input.SetJoypad2State(up, down, left, right, fireA, fireB, pause);
  }

  public void SetResetButtonState(bool reset) => _input.SetResetButtonState(reset);

  public void LoadState(string path)
  {
    _snapshotPath = path;
    _loadRequested = true;
  }

  public void SaveState(string path)
  {
    _snapshotPath = path;
    _saveRequested = true;
  }

  public void Stop() => _running = false;

  #pragma warning disable SYSLIB0011
  private Snapshot LoadSnapshot()
  {
    if (!File.Exists(_snapshotPath))
      return null;

    Snapshot state;
    using (var stream = new FileStream(_snapshotPath, FileMode.Open))
    {
      var formatter = new BinaryFormatter();
      state = (Snapshot)formatter.Deserialize(stream);
    }
    return state;
  }

  private void SaveSnapshot(Snapshot state)
  {
    using (var stream = new FileStream(_snapshotPath, FileMode.Create))
    {
      var formatter = new BinaryFormatter();
      formatter.Serialize(stream, state);
    }
  }
  #pragma warning restore SYSLIB0011
  #endregion
}