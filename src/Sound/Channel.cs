using System.Numerics;

namespace Quill.Sound;

public struct Channel
{
  #region Constants
  private const ushort INITIAL_LFSR = 0b_1000_0000_0000_0000;
  private static readonly short[] VOLUME_TABLE = new short[]
  {
    8191, 6507, 5168, 4105,
    3261, 2590, 2057, 1642,
    1298, 1031, 819,  650,
    516,  1642, 410,  0
  };
  #endregion

  #region Fields
  public byte Volume;
  public ushort Tone;
  private ushort _counter;
  private ushort _lfsr;
  private bool _polarity;

  #endregion

  public Channel()
  {
    Volume = 0xF;
    Tone = 0x0;
    _counter = 0;
    _polarity = true;
    _lfsr = INITIAL_LFSR;
  }

  #region Methods
  public short GenerateTone()
  {
    if (Tone == 0)
      return 0;

    _counter--;

    if (_counter <= 0)
    {
      _counter = Tone;
      _polarity = !_polarity;
    }

    if (_polarity)
      return VOLUME_TABLE[Volume];
    else
      return (short)-VOLUME_TABLE[Volume];
  }

  public short GenerateNoise(ushort tone2)
  {
    if (Tone == 0)
      return 0;

    _counter--;

    if (_counter <= 0)
    {
      _counter = (Tone & 0b_11) switch
      {
        0x00 => 0x10,
        0x01 => 0x20,
        0x02 => 0x30,
        0x03 => tone2
      };

      _polarity = !_polarity;
      if (_polarity)
      {
        var isWhiteNoise = ((Tone >> 2) & 1) > 0;
        var tapped = (byte)(Tone & 0b_1001);
        var end = isWhiteNoise
                ? BitOperations.PopCount((uint)(_lfsr & tapped)) % 2
                : (_lfsr & 1);
        end <<= 15;
        _lfsr = (ushort)((_lfsr >> 1) | end);
      }
    }

    return (short)(VOLUME_TABLE[Volume] * (_lfsr & 1));
  }
  #endregion
}
