using System;
using System.Runtime.CompilerServices;

using Quill.Common.Extensions;

namespace Quill.Sound;

public sealed partial class PSG
{
  public PSG()
  {
    _channels = new Channel[CHANNEL_COUNT];
    _channels[TONE0] = new Channel();
    _channels[TONE1] = new Channel();
    _channels[TONE2] = new Channel();
    _channels[NOISE] = new Channel();
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
    while (_cycleCounter >= CYCLES_PER_SAMPLE)
    {
      GenerateSample();
      _cycleCounter -= CYCLES_PER_SAMPLE;
    }
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void GenerateSample()
  {
    short sample = 0;
    sample += _channels[TONE0].GenerateTone();
    sample += _channels[TONE1].GenerateTone();
    sample += _channels[TONE2].GenerateTone();
    sample += _channels[NOISE].GenerateNoise(_channels[TONE2].Tone);
    
    OnSampleGenerated(sample);
  }
  #endregion
}
