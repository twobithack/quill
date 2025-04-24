using System.Threading;

using Quill.Common;
using Quill.CPU;
using Quill.IO;
using Quill.Sound;
using Quill.Video;

namespace Quill.Core;

unsafe public sealed class Emulator
{
  #region Constants
  private const int REWIND_BUFFER_SIZE = 1000;
  private const int FRAMES_PER_REWIND = 3;
  #endregion

  #region Fields
  private readonly PSG _audio;
  private readonly VDP _video;
  private readonly Ports _io;
  private readonly Clock _clock;
  private readonly Resampler _resampler;
  private readonly byte[] _rom;

  private readonly object _frameLock;
  private bool _frameTimeElapsed;
  private bool _running;

  private readonly RingBuffer<Snapshot> _history;
  private string _snapshotPath;
  private bool _loadRequested;
  private bool _saveRequested;
  private bool _rewinding;
  #endregion

  public Emulator(byte[] rom, Configuration config)
  { 
    _audio = new PSG();
    _video = new VDP();
    _io = new Ports();
    _clock = new Clock(config);
    _resampler = new Resampler(config);
    _rom = rom;

    _audio.OnSampleGenerated += _clock.HandleSampleGenerated;
    _audio.OnSampleGenerated += _resampler.HandleSampleGenerated;
    _clock.OnFrameTimeElapsed += HandleFrameTimeElapsed;

    _history = new RingBuffer<Snapshot>(REWIND_BUFFER_SIZE);
    _frameLock = new object();
  }

  #region Methods
  public void Run()
  {
    var cpu = new Z80(_rom, _audio, _video, _io);
    var frameCounter = 0;

    _frameTimeElapsed = true;
    _running = true;

    while (_running)
    {
      WaitForFrameTimeElapsed();

      while (!_video.FrameCompleted)
      {
        var clockCycles = cpu.Step();
        _audio.Step(clockCycles);
        _video.Step(clockCycles);
      }
      
      _video.AcknowledgeFrame();
      frameCounter++;

      if (_rewinding)
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
      else if (frameCounter >= FRAMES_PER_REWIND)
      {
        var slot = _history.AcquireSlot();
        cpu.SaveState(slot);
        frameCounter = 0;
      }
    }
  }

  public void Stop() => _running = false;

  public byte[] ReadAudioBuffer() => _resampler.ReadBuffer();

  public byte[] ReadFramebuffer() => _video.ReadFramebuffer();

  public void SetJoypadState(int joypad, JoypadState state)
  {
    if (joypad == 0)
      _io.SetJoypad1State(state);
    else
      _io.SetJoypad2State(state);
  }

  public void SetResetButtonState(bool reset) => _io.SetResetButtonState(reset);

  public void SetRewinding(bool rewinding) => _rewinding = rewinding;

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

  private void HandleFrameTimeElapsed()
  {
    lock (_frameLock)
    {
      _frameTimeElapsed = true;
      Monitor.Pulse(_frameLock);
    }
  }

  private void WaitForFrameTimeElapsed()
  {
    lock (_frameLock)
    {
      while (!_frameTimeElapsed)
        Monitor.Wait(_frameLock);

      _frameTimeElapsed = false;
    }
  }

  private Snapshot LoadSnapshot() => Snapshot.ReadFromFile(_snapshotPath);

  private void SaveSnapshot(Snapshot state) => state.WriteToFile(_snapshotPath);
  #endregion
}