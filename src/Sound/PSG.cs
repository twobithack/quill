using Quill.Common;

namespace Quill.Sound;

public sealed class PSG
{
  #region Constants
  private const int SAMPLE_RATE = 44100;
  private const int CHANNEL_COUNT = 4;
  private const int PULSE0 = 0b_00;
  private const int PULSE1 = 0b_01;
  private const int PULSE2 = 0b_10;
  private const int NOISE = 0b_11;

  #endregion

  #region Fields
  private readonly Channel[] _channels;
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
  }

  #region Methods
  public void WriteData(byte value)
  {
    if (value.MSB())
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
        var data = (byte)(value & 0b_0011_1111);
        var tone = _channels[_channelLatch].Tone;
        _channels[_channelLatch].Tone = (ushort)(((value & 0b_0011_1111) << 4) | tone.LowByte().LowNibble());
      }
    }
  }

  public byte[] ReadAudioBuffer()
  {
    var b = new byte[SAMPLE_RATE];
    System.Array.Fill(b, _channels[1].Volume);
    return b;
  }
  #endregion
}
