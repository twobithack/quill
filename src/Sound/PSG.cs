using Quill.Common;
using System.Diagnostics;

namespace Quill.Sound;

public sealed class PSG
{
  #region Constants
  private const int SAMPLE_SIZE = 3000;
  private const int MAX_VOLUME = 31;
  private const float VOLUME_STEP = 0.79432823F;
  private const int CHANNEL_COUNT = 4;
  private const int PULSE0 = 0b_00;
  private const int PULSE1 = 0b_01;
  private const int PULSE2 = 0b_10;
  private const int NOISE = 0b_11;

  #endregion

  #region Fields
  private readonly Channel[] _channels;
  private readonly byte[] _volumes;
  private int _channelLatch;
  private bool _volumeLatched;
  #endregion

  public PSG()
  {
    _channels = new Channel[CHANNEL_COUNT];
    _channels[PULSE0] = new Channel();
    _channels[PULSE1] = new Channel();
    _channels[PULSE2] = new Channel();
    _channels[NOISE] = new Channel();

    _volumes = new byte[16];
    float volume = MAX_VOLUME;
    for (int i = 0; i < 15; i++)
    {
      _volumes[i] = (byte)volume;
      volume *= VOLUME_STEP;
      Debug.WriteLine(_volumes[i]);
    }
  }

  #region Methods
  public void WriteData(byte value)
  {
    if (value.TestBit(7))
    {
      _channelLatch = (value >> 5) & 0b_11;
      _volumeLatched = value.TestBit(4);

      if (_volumeLatched)
      {
        var volume = _channels[_channelLatch].Volume;
        _channels[_channelLatch].Volume = (byte)((volume & 0b_0011_1111 << 4) | value.LowNibble());
      }
      else
      {
        var tone = _channels[_channelLatch].Tone;
        _channels[_channelLatch].Tone = (ushort)((tone & 0b_0011_1111_0000) | value.LowNibble());
      }
    }
    else
    {
      if (_volumeLatched)
      {
        _channels[_channelLatch].Volume = value.LowNibble();
      }
      else
      {
        var tone = _channels[_channelLatch].Tone;
        if (_channelLatch == NOISE)
          _channels[NOISE].Tone = value.LowNibble();
        else
          _channels[_channelLatch].Tone = (ushort)(((value & 0b_0011_1111) << 4) | tone.LowByte().LowNibble());
      }
    }
  }

  private byte[] _buffer = new byte[SAMPLE_SIZE];
  private ushort _bufferPos = 0;
  public void UpdateAudioBuffer()
  {
    lock (_buffer)
    {
      if (_bufferPos == SAMPLE_SIZE)
        return;

      for (int i = 0; i < SAMPLE_SIZE / 100; i++)
      {
        byte tone = 0;
        for (int index = 0; index < NOISE; index++)
        {
          var pulse = _channels[index];
          if (pulse.Tone == 0)
            continue;

          pulse.Counter--;

          if (pulse.Counter <= 0)
          {
            pulse.Counter = pulse.Tone;
            pulse.Polarity = !pulse.Polarity;
          }

          if (pulse.Polarity)
            tone += _volumes[pulse.Volume];
          else
            tone -= _volumes[pulse.Volume];

          _channels[index] = pulse;
        }

        _buffer[_bufferPos] = tone;
        _bufferPos++;
      }
    }
  }

  public byte[] ReadAudioBuffer()
  {
    lock (_buffer)
    {
      if (_bufferPos != SAMPLE_SIZE)
        return null;
      _bufferPos = 0;
      return _buffer;
    }
  }
  #endregion
}
