using System.Numerics;

namespace Quill.Sound;

public struct Channel
{
  #region Constants
  private const ushort INITIAL_LFSR = 0x2000;
  private const byte LFSR_TAPPED_BITS = 0b_1001;
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
        0x02 => 0x40,
        0x03 => tone2
      };

      _polarity = !_polarity;
      if (_polarity)
      {
        var isWhiteNoise = ((Tone >> 2) & 1) > 0;
        var input = isWhiteNoise
                  ? Parity(_lfsr & LFSR_TAPPED_BITS)
                  : (_lfsr & 1);
        _lfsr = (ushort)((input << 15 ) | (_lfsr >> 1));
      }
    }

    return (short)(VOLUME_TABLE[Volume] * (_lfsr & 1));
  }

  public void ResetLFSR() => _lfsr = INITIAL_LFSR;

  private static int Parity(int value) => 1 - (BitOperations.PopCount((uint)value) % 2);
  #endregion
}
