using System.Runtime.CompilerServices;

using Quill.Common.Extensions;

namespace Quill.Video;

public readonly struct Pattern(byte bp0, byte bp1, byte bp2, byte bp3)
{
  private readonly byte _bitplane0 = bp0;
  private readonly byte _bitplane1 = bp1;
  private readonly byte _bitplane2 = bp2;
  private readonly byte _bitplane3 = bp3;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public byte GetPaletteIndex(int column)
  {
    byte paletteIndex = 0x00;
    if (_bitplane0.TestBit(column)) paletteIndex |= 0b_0001;
    if (_bitplane1.TestBit(column)) paletteIndex |= 0b_0010;
    if (_bitplane2.TestBit(column)) paletteIndex |= 0b_0100;
    if (_bitplane3.TestBit(column)) paletteIndex |= 0b_1000;
    return paletteIndex;
  }
}
