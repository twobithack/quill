using System.Threading;

using Quill.Common;
using Quill.CPU;
using Quill.Input;
using Quill.Sound;
using Quill.Video;

namespace Quill.Core;

unsafe public class Emulator
{
  #region Constants
  private const int FRAME_RATE = 60;
  private const int CYCLES_PER_SCANLINE = 228;
  private const int REWIND_BUFFER_SIZE = 1000;
  private const int FRAMES_PER_REWIND = 3;
  #endregion

  #region Fields
  public bool Rewinding;
  
  private readonly IO _input;
  private readonly PSG _audio;
  private readonly VDP _video;
  private readonly byte[] _rom;

  private readonly object _frameLock;
  private bool _frameTimeElapsed;
  private bool _running;

  private readonly RingBuffer<Snapshot> _history;
  private string _snapshotPath;
  private bool _loadRequested;
  private bool _saveRequested;
  #endregion

  public Emulator(byte[] rom, int sampleRate, int virtualScanlines)
  {
    _input = new IO();
    _audio = new PSG(sampleRate, FRAME_RATE, OnFrameTimeElapsed);
    _video = new VDP();
    _rom = rom;

    _history = new RingBuffer<Snapshot>(REWIND_BUFFER_SIZE);
    _frameLock = new object();
  }

  #region Methods
  public void Run()
  {
    var cpu = new Z80(_rom, _input, _audio, _video);
    var cycleCounter = 0;
    var frameCounter = 0;

    _frameTimeElapsed = true;
    _running = true;

    while (_running)
    {
      lock (_frameLock)
      {
        while (!_frameTimeElapsed)
          Monitor.Wait(_frameLock);

        _frameTimeElapsed = false;
      }
      
      var scanlines = _video.ScanlinesPerFrame;
      while (scanlines > 0)
      {
        if (cycleCounter < CYCLES_PER_SCANLINE)
        {
          var clockCycles = cpu.Step();
          cycleCounter += clockCycles;
          _audio.Step(clockCycles);
          continue;
        }
        
        _video.RenderScanline();
        cycleCounter -= CYCLES_PER_SCANLINE;
        scanlines--;
      }

      frameCounter++;

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
      else if (frameCounter >= FRAMES_PER_REWIND)
      {
        var state = cpu.SaveState();
        _history.Push(state);
        frameCounter = 0;
      }
    }
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

  private void OnFrameTimeElapsed()
  {
    lock (_frameLock)
    {
      _frameTimeElapsed = true;
      Monitor.Pulse(_frameLock);
    }
  }

  private Snapshot LoadSnapshot() => Snapshot.ReadFromFile(_snapshotPath);

  private void SaveSnapshot(Snapshot state) => state.WriteToFile(_snapshotPath);
  #endregion
}