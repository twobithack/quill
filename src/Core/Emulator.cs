using Quill.Common;
using Quill.CPU;
using Quill.Input;
using Quill.Sound;
using Quill.Video;
using System.Diagnostics;

namespace Quill.Core;

unsafe public class Emulator
{
  #region Constants
  private const double FRAME_INTERVAL_MS = 1000d / 60d;
  private const int CYCLES_PER_SCANLINE = 228;
  private const int REWIND_BUFFER_SIZE = 1000;
  private const int FRAMES_PER_REWIND = 3;
  #endregion

  #region Fields
  public bool FastForwarding;
  public bool Rewinding;

  private readonly RingBuffer<Snapshot> _history;
  private readonly IO _input;
  private readonly PSG _audio;
  private readonly VDP _video;
  private readonly byte[] _rom;
  private string _snapshotPath;
  private bool _loadRequested;
  private bool _saveRequested;
  private bool _running;
  #endregion

  public Emulator(byte[] rom, int sampleRate, int virtualScanlines)
  {
    _history = new RingBuffer<Snapshot>(REWIND_BUFFER_SIZE);
    _input = new IO();
    _audio = new PSG(sampleRate);
    _video = new VDP(virtualScanlines);
    _rom = rom;
  }

  #region Methods
  public void Run()
  {
    var cpu = new Z80(_rom, _input, _audio, _video);
    _running = true;
    _audio.Play();

    var frameCount = 0;
    var frameTime = new Stopwatch();
    frameTime.Start();

    while (_running)
    {
      if (!FastForwarding && 
          frameTime.Elapsed.TotalMilliseconds < FRAME_INTERVAL_MS)
        continue;

      frameTime.Restart();
      frameCount++;

      var scanlines = _video.ScanlinesPerFrame;
      while (scanlines > 0)
      {
        cpu.Run(CYCLES_PER_SCANLINE);
        _video.RenderScanline();
        scanlines--;
      }

      if (Rewinding)
      {
        var state = _history.Pop();
        cpu.LoadState(state);
      }
      else if (_loadRequested)
      {
        var state = LoadSnapshot();
        cpu.LoadState(state);
        _loadRequested = false;
      }
      else if (_saveRequested)
      {
        var state = cpu.SaveState();
        SaveSnapshot(state);
        _saveRequested = false;
      }
      else if (frameCount >= FRAMES_PER_REWIND)
      {
        var state = cpu.SaveState();
        _history.Push(state);
        frameCount = 0;
      }
    }

    _audio.Stop();
  }

  public void Stop() => _running = false;

  public byte[] ReadFramebuffer() => _video.ReadFramebuffer();

  public byte[] ReadAudioBuffer() => _audio.ReadBuffer();

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

  private Snapshot LoadSnapshot() => Snapshot.ReadFromFile(_snapshotPath);

  private void SaveSnapshot(Snapshot state) => state.WriteToFile(_snapshotPath);
  #endregion
}