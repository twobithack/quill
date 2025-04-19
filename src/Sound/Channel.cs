using System.Numerics;

using Quill.Common.Extensions;

namespace Quill.Sound;

public struct Channel
{
  #region Constants
  private const ushort INITIAL_LFSR = 0x8000;
  private const byte LFSR_TAPPED_BITS = 0b_1001;
  private static readonly short[] ATTENUATION_TABLE = new short[]
  {
    8191, 6507, 5168, 4105,
    3261, 2590, 2057, 1642,
    1298, 1031, 819,  650,
    516,  410,  326,  0
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

  private bool WhiteNoiseMode => Tone.TestBit(2);

  #region Methods
  public short GenerateTone()
  {
    if (Tone <= 1)
      return ATTENUATION_TABLE[Volume];
      
    _counter--;
    if (_counter <= 0)
    {
      _counter = Tone;
      _polarity = !_polarity;
    }
    
    return _polarity 
      ? ATTENUATION_TABLE[Volume] 
      : (short)-ATTENUATION_TABLE[Volume];
  }

  public short GenerateNoise(ushort tone2)
  {
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
        var input = WhiteNoiseMode
                  ? Parity(_lfsr & LFSR_TAPPED_BITS)
                  : (_lfsr & 1);
        _lfsr = (ushort)((_lfsr >> 1) | (input << 15));
      }
    }

    return _lfsr.TestBit(0)
      ? ATTENUATION_TABLE[Volume] 
      : (short)-ATTENUATION_TABLE[Volume];
  }

  public void ResetLFSR() => _lfsr = INITIAL_LFSR;

  private static int Parity(int value)
  {
     value ^= value >> 8;
     value ^= value >> 4;
     value ^= value >> 2;
     value ^= value >> 1;
     return value & 1;
  }
  #endregion
}
