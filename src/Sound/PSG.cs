using System;
using System.Diagnostics;
using System.Threading;

using Quill.Common.Extensions;

namespace Quill.Sound;

public sealed class PSG
{
  #region Constants
  private const int MASTER_CLOCK = 3579545;
  private const int MASTER_CLOCK_DIVIDER = 16;
  private const double CLOCK_RATE = (double) MASTER_CLOCK / MASTER_CLOCK_DIVIDER;
  private const int BUFFER_SIZE = 440;
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

  private readonly short[] _buffer;
  private readonly short[] _rawBuffer;
  private readonly byte[] _copyBuffer;
  private readonly object _bufferLock;
  private volatile int _bufferPosition;
  private int _rawBufferPosition;

  private readonly int _sampleRate;
  private readonly int _decimationFactor;
  private readonly double _decimationRemainder;
  private int _masterClockCycles;
  private double _phase;

  private event Action FrameTimeElapsed;
  private readonly int _samplesPerFrame;
  private int _sampleCounter;
  #endregion

  public PSG(int sampleRate, int frameRate, Action frameTimeElapsed)
  {
    _channels = new Channel[CHANNEL_COUNT];
    _channels[TONE0] = new Channel();
    _channels[TONE1] = new Channel();
    _channels[TONE2] = new Channel();
    _channels[NOISE] = new Channel();

    _bufferLock = new object();
    _buffer = new short[BUFFER_SIZE];
    _rawBuffer = new short[BUFFER_SIZE];
    _copyBuffer = new byte[BUFFER_SIZE * 2];

    _sampleRate = sampleRate;
    _samplesPerFrame = _sampleRate / frameRate;
    FrameTimeElapsed = frameTimeElapsed;

    var ratio = CLOCK_RATE / _sampleRate;
    _decimationFactor = (int)ratio;
    _decimationRemainder = ratio - _decimationFactor;
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
    _masterClockCycles += cycles;
    while (_masterClockCycles >= MASTER_CLOCK_DIVIDER)
    {
      GenerateSample();
      _masterClockCycles -= MASTER_CLOCK_DIVIDER;
    }
    
    if (_sampleCounter == _samplesPerFrame)
    {
      FrameTimeElapsed?.Invoke();
      _sampleCounter -= _samplesPerFrame;
    }
  }

  public byte[] ReadBuffer()
  {
    lock (_bufferLock)
    {
      while (_bufferPosition < BUFFER_SIZE)
        Monitor.Wait(_bufferLock);

      Buffer.BlockCopy(_buffer, 0, _copyBuffer, 0, _copyBuffer.Length);
      _bufferPosition = 0;

      Monitor.Pulse(_bufferLock);
      return _copyBuffer;
    }
  }

  private void GenerateSample()
  {
    _rawBuffer[_rawBufferPosition] = GenerateRawSample();
    _rawBufferPosition++;

    var sampleCount = _phase >= 1
                    ? _decimationFactor + 1
                    : _decimationFactor;

    if (_rawBufferPosition == sampleCount)
    {
      var sample = 0;
      for (var index = 0; index < sampleCount; index++)
        sample += _rawBuffer[index];

      lock (_bufferLock)
      {
        while (_bufferPosition == BUFFER_SIZE)
          Monitor.Wait(_bufferLock);

        _buffer[_bufferPosition] = (short)(sample / sampleCount);
        _bufferPosition++;

        if (_bufferPosition == BUFFER_SIZE)
          Monitor.Pulse(_bufferLock);
      }

      if (_phase >= 1)
        _phase -= 1;
      
      _phase += _decimationRemainder;
      _rawBufferPosition = 0;
      _sampleCounter++;
    }
  }

  private short GenerateRawSample()
  {
    var sample = 0;
    sample += _channels[TONE0].GenerateTone();
    sample += _channels[TONE1].GenerateTone();
    sample += _channels[TONE2].GenerateTone();
    sample += _channels[NOISE].GenerateNoise(_channels[TONE2].Tone);
    return (short)sample;
  }
  #endregion
}
