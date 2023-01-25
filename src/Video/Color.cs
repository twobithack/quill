using System.Runtime.CompilerServices;

namespace Quill.Video;

public static class Color
{
  #region Constants
  private const byte MAX_VALUE = 0b_11;
  private const byte MULTIPLIER = byte.MaxValue / MAX_VALUE;

  private readonly static int[] LEGACY_PALETTE = new int[]
  {
    0x000000, 0x000000, 0x42C821, 0x78DC5E, 
    0xED5554, 0xFC767D, 0x4D52D4, 0xF5EB42,
    0x5455FC, 0x7879FF, 0x54C1D4, 0x54CEE6, 
    0x3BB021, 0xBA5BC9, 0xCCCCCC, 0xFFFFFF
  };
  #endregion

  #region Methods
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static int ToRGBA(byte color)
  {
    var r = (color & MAX_VALUE) * MULTIPLIER;
    color >>= 2;
    var g = (color & MAX_VALUE) * MULTIPLIER;
    color >>= 2;
    var b = (color & MAX_VALUE) * MULTIPLIER;
    return (r << 0) | (g << 8) | (b << 16);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static int ToLegacyRGBA(byte color) => LEGACY_PALETTE[color];
  #endregion
}
