using System;

namespace Quill.Video;

[Serializable]
public struct Color
{
  private const byte BITMASK = 0b_0011;
  private const byte MULTIPLIER = byte.MaxValue / 3;

  public byte Red = 0x00;
  public byte Green = 0x00;
  public byte Blue = 0x00;

  public Color() { }

  public void Set(byte color)
  {
    Red = (byte)((color & BITMASK) * MULTIPLIER);
    color >>= 2;
    Green = (byte)((color & BITMASK) * MULTIPLIER);
    color >>= 2;
    Blue = (byte)((color & BITMASK) * MULTIPLIER);
  }
}
