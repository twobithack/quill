using Quill.Common;
using System.Runtime.CompilerServices;

namespace Quill.Video;

public readonly struct Pattern
{
  private readonly byte Row0 = 0x00;
  private readonly byte Row1 = 0x00;
  private readonly byte Row2 = 0x00;
  private readonly byte Row3 = 0x00;

  public Pattern(byte row0, byte row1, byte row2, byte row3)
  {
    Row0 = row0;
    Row1 = row1;
    Row2 = row2;
    Row3 = row3;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public byte GetPaletteIndex(int column)
  {
    byte paletteIndex = 0x00;
    if (Row0.TestBit(column)) paletteIndex |= 0b_0001;
    if (Row1.TestBit(column)) paletteIndex |= 0b_0010;
    if (Row2.TestBit(column)) paletteIndex |= 0b_0100;
    if (Row3.TestBit(column)) paletteIndex |= 0b_1000;
    return paletteIndex;
  }
}
