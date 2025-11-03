using System;
using System.Runtime.CompilerServices;
using System.Threading;

using Quill.Common;
using Quill.Common.Interfaces;
using Quill.Sound;

namespace Quill.Core;

public sealed class Resampler : IAudioSink
{
  #region Fields
  private readonly object _bufferLock;
  private readonly short[] _buffer;
  private readonly byte[] _copyBuffer;
  private readonly int _bufferSize;
  private volatile int _bufferPosition;

  private readonly double _decimationFactor;
  private double _phase;

  private int _rawSampleAccumulator;
  private int _rawSampleCounter;
  private int _rawSamplesNeeded;
  #endregion

  public Resampler(Configuration config)
  {
    _bufferLock = new object();
    _bufferSize = config.AudioBufferSize;
    _buffer = new short[_bufferSize];
    _copyBuffer = new byte[_bufferSize * 2];

    var rawSampleRate = (double) config.ClockRate / PSG.CYCLES_PER_SAMPLE;
    _decimationFactor = rawSampleRate / config.AudioSampleRate;
    _rawSamplesNeeded = (int)_decimationFactor;
  }
  
  #region Methods
  public void EnqueueSample(short rawSample)
  {
    _rawSampleAccumulator += rawSample;
    _rawSampleCounter++;

    if (_rawSampleCounter == _rawSamplesNeeded)
      GenerateDecimatedSample();
  }

  public byte[] ReadBuffer()
  {
    lock (_bufferLock)
    {
      while (_bufferPosition < _bufferSize)
        Monitor.Wait(_bufferLock);

      Buffer.BlockCopy(_buffer, 0, _copyBuffer, 0, _copyBuffer.Length);
      _bufferPosition = 0;

      Monitor.Pulse(_bufferLock);
      return _copyBuffer;
    }
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void GenerateDecimatedSample()
  {
    lock (_bufferLock)
    {
      while (_bufferPosition == _bufferSize)
        Monitor.Wait(_bufferLock);

      _buffer[_bufferPosition] = (short)(_rawSampleAccumulator / _rawSampleCounter);
      _bufferPosition++;

      if (_bufferPosition == _bufferSize)
        Monitor.Pulse(_bufferLock);
    }

    _phase += _decimationFactor - _rawSamplesNeeded;
    _rawSamplesNeeded = (int)(_decimationFactor + _phase);
    _rawSampleAccumulator = 0;
    _rawSampleCounter = 0;
  }
  #endregion
}