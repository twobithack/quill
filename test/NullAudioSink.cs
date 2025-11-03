using Quill.Common.Interfaces;

namespace Quill.Tests;

public class NullAudioSink : IAudioSink
{
  public void EnqueueSample(short sample) { }

  public byte[] ReadBuffer() => [];
}