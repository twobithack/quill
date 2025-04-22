using System;

using Quill.Common.Extensions;

namespace Quill.Sound;

public sealed class PSG
{
  #region Constants
  private const int MASTER_CLOCK_DIVIDER = 16;
  private const int CHANNEL_COUNT = 4;
  private const int TONE0 = 0b_00;
  private const int TONE1 = 0b_01;
  private const int TONE2 = 0b_10;
  private const int NOISE = 0b_11;
  #endregion

  #region Fields
  private readonly Channel[] _channels;
  private int _channelLatch;
  private bool _volumeLatch;

  private readonly Action<short> _onSampleGenerated;
  private int _cycleCounter;
  #endregion

  public PSG(Action<short> onSampleGenerated)
  {
    _channels = new Channel[CHANNEL_COUNT];
    _channels[TONE0] = new Channel();
    _channels[TONE1] = new Channel();
    _channels[TONE2] = new Channel();
    _channels[NOISE] = new Channel();

    _onSampleGenerated = onSampleGenerated;
  }

  #region Methods
  public void WriteData(byte value)
  {
    if (value.TestBit(7))
    {
      _channelLatch = (value >> 5) & 0b_11;
      _volumeLatch = value.TestBit(4);

      if (_volumeLatch)
      {
        _channels[_channelLatch].Volume = value.LowNibble();
        return;
      }

      if (_channelLatch == NOISE)
      {
        _channels[NOISE].Tone = value.LowNibble();
        _channels[NOISE].Tone &= 0b_0111;
        _channels[NOISE].ResetLFSR();
        return;
      }

      _channels[_channelLatch].Tone &= 0b_0011_1111_0000;
      _channels[_channelLatch].Tone |= value.LowNibble();
    }
    else
    {
      if (_volumeLatch)
      {
        _channels[_channelLatch].Volume = value.LowNibble();
        return;
      }

      if (_channelLatch == NOISE)
      {
        _channels[NOISE].Tone = value.LowNibble();
        _channels[NOISE].Tone &= 0b_0111;
        _channels[NOISE].ResetLFSR();
        return;
      }

      _channels[_channelLatch].Tone &= 0b_0000_0000_1111;
      _channels[_channelLatch].Tone |= (ushort)((value & 0b_0011_1111) << 4);
    }
  }

  public void Step(int cycles)
  {
    _cycleCounter += cycles;
    while (_cycleCounter >= MASTER_CLOCK_DIVIDER)
    {
      GenerateSample();
      _cycleCounter -= MASTER_CLOCK_DIVIDER;
    }
  }

  private void GenerateSample()
  {
    short sample = 0;
    sample += _channels[TONE0].GenerateTone();
    sample += _channels[TONE1].GenerateTone();
    sample += _channels[TONE2].GenerateTone();
    sample += _channels[NOISE].GenerateNoise(_channels[TONE2].Tone);
    
    _onSampleGenerated.Invoke(sample);
  }
  #endregion
}
