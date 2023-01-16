using Quill.CPU;
using Quill.Input;
using Quill.Sound;
using Quill.Video;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Quill.Core;

unsafe public class Emulator
{
  #region Constants
  private const double FRAME_INTERVAL_MS = 1000d / 60d;
  private const int CYCLES_PER_SCANLINE = 228;
  #endregion

  #region Fields
  private readonly IO _input;
  private readonly PSG _sound;
  private readonly VDP _video;
  private readonly byte[] _rom;
  private string _snapshotPath;
  private bool _loadRequested;
  private bool _saveRequested;
  private bool _running;
  #endregion

  public Emulator(byte[] rom, int extraScanlines)
  {
    _input = new IO();
    _sound = new PSG();
    _video = new VDP(extraScanlines);
    _rom = rom;
  }

  #region Methods
  public void Run()
  {
    var cpu = new Z80(_rom, _input, _sound, _video);
    var clock = new Stopwatch();
    var lastFrame = 0d;

    clock.Start();
    _sound.Start();

    _running = true;
    while (_running)
    {
      var currentTime = clock.Elapsed.TotalMilliseconds;
      if (currentTime < lastFrame + FRAME_INTERVAL_MS)
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
    _sound.Stop();
  }

  public void Stop() => _running = false;

  public byte[] ReadFramebuffer() => _video.ReadFramebuffer();

  public byte[] ReadAudioBuffer() => _sound.ReadBuffer();

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

  #pragma warning disable SYSLIB0011
  private Snapshot LoadSnapshot()
  {
    if (!File.Exists(_snapshotPath))
      return null;

    Snapshot state;
    try
    {
      using var stream = new FileStream(_snapshotPath, FileMode.Open);
      var formatter = new BinaryFormatter();
      state = (Snapshot)formatter.Deserialize(stream);
    }
    catch
    { 
      return null;
    }

    return state;
  }

  private void SaveSnapshot(Snapshot state)
  {
    using var stream = new FileStream(_snapshotPath, FileMode.Create);
    var formatter = new BinaryFormatter();
    formatter.Serialize(stream, state);
  }
  #pragma warning restore SYSLIB0011
  #endregion
}