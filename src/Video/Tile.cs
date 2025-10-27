using Quill.Common.Extensions;

namespace Quill.Video;

public readonly struct Tile(ushort data)
{
  private readonly ushort _data = data;

  #region Properties
  public int  PatternIndex     => _data & 0b_0000_0001_1111_1111;
  public bool HorizontalFlip   => _data.TestBit(9);
  public bool VerticalFlip     => _data.TestBit(10);
  public bool UseSpritePalette => _data.TestBit(11);
  public bool HighPriority     => _data.TestBit(12);
  #endregion
}
