using Quill.Common.Interfaces;

namespace Quill.Tests;

public class NullAudioSink : IAudioSink
{
  public void SubmitSample(short sample) { }

  public byte[] ReadBuffer() => [];
}