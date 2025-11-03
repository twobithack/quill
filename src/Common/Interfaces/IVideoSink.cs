namespace Quill.Common.Interfaces;

public interface IVideoSink
{
  void BlitScanline(int y, int[] scanline);
  void PresentFrame();
  byte[] ReadFrame();
}