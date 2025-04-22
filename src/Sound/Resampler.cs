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

  private readonly int _decimationFactor;
  private readonly double _decimationRemainder;
  private int _rawSampleAccumulator;
  private int _rawSampleCounter;
  private double _phase;
  #endregion

  public Resampler(Configuration config)
  {
    _bufferLock = new object();
    _buffer = new short[BUFFER_SIZE];
    _copyBuffer = new byte[BUFFER_SIZE * 2];

    var rawSampleRate = (double) Clock.CYCLES_PER_SECOND / PSG.CYCLES_PER_SAMPLE;
    var ratio = rawSampleRate / config.AudioSampleRate;
    _decimationFactor = (int)ratio;
    _decimationRemainder = ratio - _decimationFactor;
  }

  private int RawSamplesNeeded => _phase >= 1
                                ? _decimationFactor + 1
                                : _decimationFactor;

  #region Methods
  public void HandleSampleGenerated(short rawSample)
  {
    _rawSampleAccumulator += rawSample;
    _rawSampleCounter++;

    if (_rawSampleCounter == RawSamplesNeeded)
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

    if (_phase >= 1)
      _phase -= 1;
    
    _phase += _decimationRemainder;
    _rawSampleAccumulator = 0;
    _rawSampleCounter = 0;
  }
  #endregion
}