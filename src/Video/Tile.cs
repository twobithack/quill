using Quill.Common.Extensions;

namespace Quill.Video;

public readonly struct Tile
{
  private readonly ushort Data;
  public Tile(ushort data) => Data = data;
  public int PatternIndex => Data & 0b_0000_0001_1111_1111;
  public bool HorizontalFlip => Data.TestBit(9);
  public bool VerticalFlip => Data.TestBit(10);
  public bool UseSpritePalette => Data.TestBit(11);
  public bool HighPriotity => Data.TestBit(12);
}
