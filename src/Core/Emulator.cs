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
  #region Constants
  private const int REWIND_BUFFER_SIZE = 1000;
  private const int FRAMES_PER_REWIND = 3;
  #endregion

  #region Fields
  private readonly Resampler _resampler;
  private readonly PSG _psg;
  private readonly VDP _vdp;
  private readonly Ports _ports;
  private readonly byte[] _rom;

  private readonly RingBuffer<Snapshot> _history;
  private readonly string _snapshotPath;

  private volatile bool _running;
  private volatile bool _rewinding;
  private volatile bool _loadRequested;
  private volatile bool _saveRequested;
  private volatile bool _savingEnabled;
  #endregion

  public Emulator(byte[] rom, string savePath, Configuration config)
  {
    _resampler = new Resampler(config);
    _psg = new PSG(_resampler.HandleSampleGenerated);
    _vdp = new VDP();
    _ports = new Ports();
    _rom = rom;

    _history = new RingBuffer<Snapshot>(REWIND_BUFFER_SIZE);
    _snapshotPath = savePath;
  }

  #region Methods
  public void Run()
  {
    var memory = new Mapper(_rom);
    var bus = new Bus(memory, _ports, _psg, _vdp);
    var cpu = new Z80(bus);
    var frameCounter = 0;

    _running = true;
    while (_running)
    {
      while (!_vdp.FrameCompleted())
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

  public byte[] ReadFramebuffer() => _vdp.ReadFramebuffer();

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
      _loadRequested = true;
    else if (input.IsButtonDown(Commands.Quicksave))
      _saveRequested = true;
  }

  private Snapshot LoadSnapshot() => Snapshot.ReadFromFile(_snapshotPath);

  private void SaveSnapshot(Snapshot state) => state.WriteToFile(_snapshotPath);
  #endregion
}