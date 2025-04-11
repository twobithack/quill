﻿using Quill.Common.Extensions;
using System;
using System.Diagnostics;
using System.Threading;

namespace Quill.Sound;

public sealed class PSG
{
  #region Constants
  private const int CLOCK_RATE = 223722;
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

  private readonly Thread _bufferingThread;
  private readonly short[] _buffer;
  private readonly int _sampleRate;
  private readonly int _decimationFactor;
  private readonly int _compensationInterval;
  
  private int _bufferPosition;
  private bool _playing;
  #endregion

  public PSG(int sampleRate)
  {
    _channels = new Channel[CHANNEL_COUNT];
    _channels[TONE0] = new Channel();
    _channels[TONE1] = new Channel();
    _channels[TONE2] = new Channel();
    _channels[NOISE] = new Channel();

    _buffer = new short[BUFFER_SIZE];
    _bufferPosition = BUFFER_SIZE;
    _bufferingThread = new Thread(FillBuffer);

    _sampleRate = sampleRate;
    _decimationFactor = CLOCK_RATE / _sampleRate;
    _compensationInterval = CalculateCompensationInterval();
  }

  private int CalculateCompensationInterval()
  {
    double ratio = (double)CLOCK_RATE / _sampleRate;
    double fractionalPart = ratio - Math.Floor(ratio);
    
    if (fractionalPart == 0)
        return int.MaxValue;

    double spacing = 1.0 / fractionalPart;
    return (int)Math.Round(spacing);
  }

  #region Methods
  public void Play()
  {
    _playing = true;
    _bufferingThread.Start();
  }

  public void Stop() => _playing = false;

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

  public byte[] ReadBuffer()
  {
    if (_bufferPosition == 0)
      return null;

    lock (_buffer)
    {
      var byteBuffer = new byte[_bufferPosition * 2];
      Buffer.BlockCopy(_buffer, 0, byteBuffer, 0, byteBuffer.Length);
      _bufferPosition = 0;
      return byteBuffer;
    }
  }

  private void FillBuffer()
  {
    Stopwatch timer = new Stopwatch();
    long nextSampleTick = 0;
    long sampleCount = 0;

    timer.Start();

    while (_playing)
    {
      if (timer.ElapsedTicks >= nextSampleTick)
      {
        GenerateSample(sampleCount % _compensationInterval == 0);
        sampleCount++;
        nextSampleTick = sampleCount * Stopwatch.Frequency / _sampleRate;
      }
    }
  }

  private void GenerateSample(bool addCompensationSample)
  {
    if (_bufferPosition == BUFFER_SIZE)
      return;

    int tone = 0;
    var sampleCount = addCompensationSample 
                    ? _decimationFactor + 1 
                    : _decimationFactor;

    for (var sample = 0; sample < sampleCount; sample++)
    {
      for (var channel = 0; channel < NOISE; channel++)
        tone += _channels[channel].GenerateTone();
      tone += _channels[NOISE].GenerateNoise(_channels[TONE2].Tone);
    }

    lock (_buffer)
    {
      _buffer[_bufferPosition] = (short)(tone / sampleCount);
      _bufferPosition++;
    }
  }
  #endregion
}
