namespace Quill.Common;

public class Configuration
{
  public int AudioSampleRate { get; set; } = 44100;
  public bool CropBottomBorder { get; set; } = true;
  public bool CropLeftBorder { get; set; } = true;
  public int FrameRate { get; set; } = 60;
  public int ScaleFactor { get; set; } = 1;
}