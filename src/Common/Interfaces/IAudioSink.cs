namespace Quill.Common.Interfaces;

public interface IAudioSink
{
  void SubmitSample(short sample);

  byte[] ReadBuffer();
}