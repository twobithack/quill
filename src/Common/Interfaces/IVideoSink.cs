namespace Quill.Common.Interfaces;

public interface IVideoSink
{
  bool IsOccupied(int x, int y);
  void SubmitPixel(int x, int y, int value, bool isSprite);
  void PublishFrame();
  byte[] ReadFrame();
}