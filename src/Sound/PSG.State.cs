using System;

using Quill.Core;

namespace Quill.Sound;

public sealed partial class PSG
{
  #region Constants
  public const int CYCLES_PER_SAMPLE = 16;
  public const int CHANNEL_COUNT = 4;

  private const int TONE0 = 0b_00;
  private const int TONE1 = 0b_01;
  private const int TONE2 = 0b_10;
  private const int NOISE = 0b_11;
  #endregion

  #region Fields
  public event Action<short> OnSampleGenerated;

  private readonly Channel[] _channels;
  private int _channelLatch;
  private bool _volumeLatch;
  private int _cycleCounter;
  #endregion

  public void LoadState(Snapshot state)
  {
    for (var channel = 0; channel < CHANNEL_COUNT; channel++)
    {
      _channels[channel].Tone = state.Tones[channel];
      _channels[channel].Volume = state.Volumes[channel];
    }
    _channelLatch = state.ChannelLatch;
    _volumeLatch = state.VolumeLatch;
  }

  public void SaveState(Snapshot state)
  {
    for (var channel = 0; channel < CHANNEL_COUNT; channel++)
    {
      state.Tones[channel] = _channels[channel].Tone;
      state.Volumes[channel] = _channels[channel].Volume;
    }
    state.ChannelLatch = _channelLatch;
    state.VolumeLatch = _volumeLatch;
  }
}
