namespace Quill.Common.Interfaces;

public interface IAudioSink
{
  void EnqueueSample(short sample);
  byte[] ReadBuffer();
}