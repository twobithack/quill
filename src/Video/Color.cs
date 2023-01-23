using System;

namespace Quill.Video;

[Serializable]
public struct Color
{
  #region Constants
  private const byte BITMASK = 0b_0011;
  private const byte MULTIPLIER = byte.MaxValue / 3;
  private readonly static Color[] LEGACY_PALETTE = new Color[]
  {
    new Color(0,    0,    0),
    new Color(33,   200,  66),
    new Color(94,   220,  120),
    new Color(84,   85,   237),
    new Color(125,  118,  252),
    new Color(212,  82,   77),
    new Color(66,   235,  245),
    new Color(252,  85,   84),
    new Color(255,  121,  120),
    new Color(212,  193,  84),
    new Color(230,  206,  84),
    new Color(33,   176,  59),
    new Color(201,  91,   186),
    new Color(204,  204,  204),
    new Color(255,  255,  255)
  };
  #endregion

  #region Fields
  public byte Red = 0x00;
  public byte Green = 0x00;
  public byte Blue = 0x00;
  #endregion

  public Color() { }
  public Color(byte r, byte g, byte b)
  {
    Red = r;
    Green = g;
    Blue = b;
  }

  public void Set(byte color)
  {
    Red = (byte)((color & BITMASK) * MULTIPLIER);
    color >>= 2;
    Green = (byte)((color & BITMASK) * MULTIPLIER);
    color >>= 2;
    Blue = (byte)((color & BITMASK) * MULTIPLIER);
  }

  public static Color GetLegacyColor(byte index) => LEGACY_PALETTE[index];
}
