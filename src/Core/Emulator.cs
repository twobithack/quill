using Quill.Common;
using Quill.Common.Definitions;
using Quill.CPU;
using Quill.IO;
using Quill.Memory;
using Quill.Sound;
using Quill.Video;

namespace Quill.Core;

unsafe public sealed class Emulator
{
  #region Fields
  private readonly Framebuffer _framebuffer;
  private readonly Resampler _resampler;
  private readonly Ports _ports;
  private readonly byte[] _rom;

  private readonly RingBuffer<Snapshot> _history;
  private readonly int _rewindInterval;
  private readonly string _savePath;

  private volatile bool _running;
  private volatile bool _rewinding;
  private volatile bool _loadRequested;
  private volatile bool _saveRequested;
  private volatile bool _savingEnabled;
  #endregion

  public Emulator(byte[] rom, string savePath, Configuration config)
  {
    _framebuffer = new Framebuffer();
    _resampler = new Resampler(config);
    _ports = new Ports();
    _rom = rom;

    _history = new RingBuffer<Snapshot>(config.Rewind.SnapshotCount);
    _rewindInterval = config.Rewind.FrameInterval;
    _savePath = savePath;
  }

  #region Methods
  public void Run()
  {
    var memory = new Mapper(_rom);
    var psg = new PSG(_resampler);
    var vdp = new VDP(_framebuffer);
    var bus = new Bus(memory, _ports, psg, vdp);
    var cpu = new Z80(bus);
    var frameCounter = 0;

    _running = true;
    while (_running)
    {
      while (!vdp.FrameCompleted())
        cpu.Step();

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
      else if (frameCounter >= _rewindInterval)
      {
        var slot = _history.AcquireSlot();
        cpu.SaveState(slot);
        frameCounter = 0;
      }
    }
  }

  public void Stop() => _running = false;

  public byte[] ReadAudioBuffer() => _resampler.ReadBuffer();

  public byte[] ReadFramebuffer() => _framebuffer.ReadFrame();

  public void UpdateInput(InputState input)
  {
    _ports.UpdateInput(input);
    _rewinding = input.IsButtonDown(Commands.Rewind);

    if (!_savingEnabled)
    {
      _savingEnabled = !input.IsButtonDown(Commands.Quickload) &&
                       !input.IsButtonDown(Commands.Quicksave);
      return;
    }

    if (input.IsButtonDown(Commands.Quickload))
    {
      _loadRequested = true;
      _savingEnabled = false;
    }
    else if (input.IsButtonDown(Commands.Quicksave))
    {
      _saveRequested = true;
      _savingEnabled = false;
    }
  }

  private Snapshot LoadSnapshot() => Snapshot.ReadFromFile(_savePath);

  private void SaveSnapshot(Snapshot state) => state.WriteToFile(_savePath);
  #endregion
}