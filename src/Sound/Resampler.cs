using System;
using System.Threading;
using Quill.Common;

namespace Quill.Sound;

public sealed class Resampler
{
  #region Constants
  private const int MASTER_CLOCK = 3579545;
  private const int MASTER_CLOCK_DIVIDER = 16;
  private const double CLOCK_RATE = (double) MASTER_CLOCK / MASTER_CLOCK_DIVIDER;
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

  private readonly Action _onFrameTimeElapsed;
  private readonly int _samplesPerFrame;
  private int _sampleCounter;
  #endregion

  public Resampler(Action onFrameTimeElapsed, Configuration config)
  {
    _bufferLock = new object();
    _buffer = new short[BUFFER_SIZE];
    _copyBuffer = new byte[BUFFER_SIZE * 2];

    _samplesPerFrame = config.AudioSampleRate / config.FrameRate;
    _onFrameTimeElapsed = onFrameTimeElapsed;

    var ratio = CLOCK_RATE / config.AudioSampleRate;
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

    if (_rawSampleCounter != RawSamplesNeeded)
      return;

    GenerateDecimatedSample();

    if (_sampleCounter == _samplesPerFrame)
    {
      _onFrameTimeElapsed.Invoke();
      _sampleCounter = 0;
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
    _sampleCounter++;
  }
  #endregion
}