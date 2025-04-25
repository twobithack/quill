namespace Quill.Common;

public class Configuration
{
  public int AudioSampleRate { get; set; } = 44100;
  public int ClockRate { get; set; } = 3579545;
  public bool CropBottomBorder { get; set; } = true;
  public bool CropLeftBorder { get; set; } = true;
  public bool FixAspectRatio { get; set; } = true;
  public int FramesPerSecond { get; set; } = 60;
  public int ScaleFactor { get; set; } = 1;
}