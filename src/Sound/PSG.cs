using Quill.Common;
using System;
using System.Diagnostics;
using System.ServiceModel.Channels;
using System.Threading;

namespace Quill.Sound;

public sealed class PSG
{
  #region Constants
  private const int CLOCK_RATE = 224000;
  private const int SAMPLE_RATE = 44100;
  private const int UPDATE_INTERVAL_MS = 1;
  private const int BUFFER_SIZE = 1000;
  private const int CHANNEL_COUNT = 4;
  private const int TONE0 = 0b_00;
  private const int TONE1 = 0b_01;
  private const int TONE2 = 0b_10;
  private const int NOISE = 0b_11;
  #endregion

  #region Fields
  private readonly BufferManager _bufferPool;
  private readonly Thread _bufferThread;
  private readonly short[] _buffer;
  private int _bufferPosition;

  private readonly Channel[] _channels;
  private int _latchedChannel;
  private bool _volumeLatched;
  private bool _playing;
  #endregion

  public PSG()
  {
    _channels = new Channel[CHANNEL_COUNT];
    _channels[TONE0] = new Channel();
    _channels[TONE1] = new Channel();
    _channels[TONE2] = new Channel();
    _channels[NOISE] = new Channel();

    _bufferPosition = 0;
    _buffer = new short[BUFFER_SIZE];
    _bufferPool = BufferManager.CreateBufferManager(BUFFER_SIZE * 2, BUFFER_SIZE * 2);
    _bufferThread = new Thread(ManageBuffer);
  }

  #region Methods
  public void Start()
  {
    _playing = true;
    _bufferThread.Start();
  }

  public void Stop() => _playing = false;

  public void WriteData(byte value)
  {
    if (value.TestBit(7))
    {
      _latchedChannel = (value >> 5) & 0b_11;
      _volumeLatched = value.TestBit(4);

      if (_volumeLatched)
      {
        _channels[_latchedChannel].Volume = value.LowNibble();
      }
      else
      {
        _channels[_latchedChannel].Tone &= 0b_0011_1111_0000;
        _channels[_latchedChannel].Tone |= value.LowNibble();
      }
    }
    else
    {
      if (_volumeLatched)
      {
        _channels[_latchedChannel].Volume = value.LowNibble();
      }
      else
      {
        if (_latchedChannel == NOISE)
        {
          _channels[NOISE].Tone = value.LowNibble();
          return;
        }

        _channels[_latchedChannel].Tone &= 0b_0000_0000_1111;
        _channels[_latchedChannel].Tone |= (ushort)((value & 0b_0011_1111) << 4);
      }
    }
  }

  public byte[] ReadBuffer()
  {
    if (_bufferPosition == 0)
      return null;

    lock (_buffer)
    {
      var samples = _bufferPool.TakeBuffer(_bufferPosition * 2);
      Buffer.BlockCopy(_buffer, 0, samples, 0, samples.Length);
      _bufferPosition = 0;
      return samples;
    }
  }

  private void ManageBuffer()
  {
    var clock = new Stopwatch();
    var lastUpdate = 0d;
    clock.Start();

    while (_playing)
    {
      if (_bufferPosition == BUFFER_SIZE)
        continue;

      var currentTime = clock.Elapsed.TotalMilliseconds;
      if (currentTime < lastUpdate + UPDATE_INTERVAL_MS)
        continue;

      lock (_buffer)
      {
        for (int i = 0; i < BUFFER_SIZE / 2; i++)
        {
          short tone = 0;
          for (int index = 0; index < NOISE; index++)
            tone += _channels[index].GenerateTone();

          tone += _channels[NOISE].GenerateNoise(_channels[TONE2].Tone);

          if (i % 5 == 0)
            _buffer[_bufferPosition++] = tone;
        }
      }
    }
  }
  #endregion
}
