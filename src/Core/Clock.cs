using System;

using Quill.Common;
using Quill.Sound;

namespace Quill.Core;

public sealed class Clock
{
  #region Constants
  public const int CYCLES_PER_SECOND = 3579545;
  #endregion

  #region Fields
  public event Action OnFrameTimeElapsed;

  private readonly double _samplesPerFrame;
  private double _sampleCounter;
  #endregion

  public Clock(Configuration config)
  {
    var samplesPerSecond = (double) CYCLES_PER_SECOND / PSG.CYCLES_PER_SAMPLE;
    _samplesPerFrame = samplesPerSecond / config.FramesPerSecond;
  }

  #region Methods
  public void HandleSampleGenerated(short _)
  {
    _sampleCounter++;

    if (_sampleCounter < _samplesPerFrame)
      return;

    _sampleCounter -= _samplesPerFrame;
    OnFrameTimeElapsed();
  }
  #endregion
}