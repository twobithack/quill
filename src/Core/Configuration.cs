namespace Quill.Core;

public class Configuration
{
  public bool CropBottomBorder { get; set; } = true;
  public bool CropLeftBorder { get; set; } = true;
  public int ExtraScanlines { get; set; } = 0;
  public int ScaleFactor { get; set; } = 1;
}