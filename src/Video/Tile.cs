using Quill.Common.Extensions;

namespace Quill.Video;

public readonly struct Tile
{
  private readonly ushort _data;

  public Tile(ushort data)
  {
    _data = data;
  }
  
  public int PatternIndex =>      _data & 0b_0000_0001_1111_1111;
  public bool HorizontalFlip =>   _data.TestBit(9);
  public bool VerticalFlip =>     _data.TestBit(10);
  public bool UseSpritePalette => _data.TestBit(11);
  public bool HighPriotity =>     _data.TestBit(12);
}
