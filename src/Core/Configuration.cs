namespace Quill.Core;

public class Configuration
{
  public bool CropBottomBorder { get; set; } = true;
  public bool CropLeftBorder { get; set; } = true;
  public int ScaleFactor { get; set; } = 1;
  public int VirtualScanlines { get; set; } = 0;
}