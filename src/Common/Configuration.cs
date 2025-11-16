namespace Quill.Common;

public readonly record struct Configuration
{
  public AudioOptions   Audio   { get; init; }
  public DisplayOptions Display { get; init; }
  public RewindOptions  Rewind  { get; init; }
  public SystemOptions  System  { get; init; }
}

public readonly record struct AudioOptions
{
  public int BufferCount        { get; init; }
  public int BufferSize         { get; init; }
  public int SampleRate         { get; init; }
}

public readonly record struct DisplayOptions
{
  public bool CropBottomBorder  { get; init; }
  public bool CropLeftBorder    { get; init; }
  public bool EnableCRTFilter   { get; init; }
  public bool FixAspectRatio    { get; init; }
  public int  ScaleFactor       { get; init; }
}

public readonly record struct RewindOptions
{
  public int SnapshotCount      { get; init; }
  public int FrameInterval      { get; init; }
}

public readonly record struct SystemOptions
{
  public int ClockRate          { get; init; }
}
