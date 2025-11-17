using Quill.Common.Interfaces;
using Quill.Core;

namespace Quill.Sound;

public sealed partial class PSG
{
  #region Fields
  private readonly IAudioSink _audioSink;
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
