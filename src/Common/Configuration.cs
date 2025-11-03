namespace Quill.Common;

public class Configuration
{
  public int  AudioBufferCount  { get; set; } = 150; 
  public int  AudioBufferSize   { get; set; } = 49;
  public int  AudioSampleRate   { get; set; } = 44100;
  public int  ClockRate         { get; set; } = 3579540;
  public bool CropBottomBorder  { get; set; } = true;
  public bool CropLeftBorder    { get; set; } = true;
  public bool EnableCRTFilter   { get; set; } = true;
  public bool FixAspectRatio    { get; set; } = true;
  public int  ScaleFactor       { get; set; } = 3;
}