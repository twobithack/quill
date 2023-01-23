using Quill.Common.Extensions;
using System.Runtime.CompilerServices;

namespace Quill.Video;

public readonly struct Pattern
{
  private readonly byte Bitplane0;
  private readonly byte Bitplane1;
  private readonly byte Bitplane2;
  private readonly byte Bitplane3;

  public Pattern(byte bp0, byte bp1, byte bp2, byte bp3)
  {
    Bitplane0 = bp0;
    Bitplane1 = bp1;
    Bitplane2 = bp2;
    Bitplane3 = bp3;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public byte GetPaletteIndex(int column)
  {
    byte paletteIndex = 0x00;
    if (Bitplane0.TestBit(column)) paletteIndex |= 0b_0001;
    if (Bitplane1.TestBit(column)) paletteIndex |= 0b_0010;
    if (Bitplane2.TestBit(column)) paletteIndex |= 0b_0100;
    if (Bitplane3.TestBit(column)) paletteIndex |= 0b_1000;
    return paletteIndex;
  }
}
