using System;
using System.Runtime.CompilerServices;
using System.Threading;

using Quill.Common;
using Quill.Core;

namespace Quill.Sound;

public sealed class Resampler
{
  #region Constants
  private const int BUFFER_SIZE = 440;
  #endregion

  #region Fields
  private readonly object _bufferLock;
  private readonly short[] _buffer;
  private readonly byte[] _copyBuffer;
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
    _buffer = new short[BUFFER_SIZE];
    _copyBuffer = new byte[BUFFER_SIZE * 2];

    var rawSampleRate = (double) Clock.CYCLES_PER_SECOND / PSG.CYCLES_PER_SAMPLE;
    _decimationFactor = rawSampleRate / config.AudioSampleRate;
    _rawSamplesNeeded = (int)_decimationFactor;
  }
  
  #region Methods
  public void HandleSampleGenerated(short rawSample)
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
      while (_bufferPosition < BUFFER_SIZE)
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
      while (_bufferPosition == BUFFER_SIZE)
        Monitor.Wait(_bufferLock);

      _buffer[_bufferPosition] = (short)(_rawSampleAccumulator / _rawSampleCounter);
      _bufferPosition++;

      if (_bufferPosition == BUFFER_SIZE)
        Monitor.Pulse(_bufferLock);
    }

    _phase += _decimationFactor - _rawSamplesNeeded;
    _rawSamplesNeeded = (int)(_decimationFactor + _phase);
    _rawSampleAccumulator = 0;
    _rawSampleCounter = 0;
  }
  #endregion
}