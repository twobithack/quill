namespace Quill.Common.Interfaces;

public interface IVideoSink
{
  void SubmitPixel(int x, int y, int color);
  void PublishFrame();
  byte[] ReadFrame();
}