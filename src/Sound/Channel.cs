namespace Quill.Sound;

public struct Channel
{
  #region Constants
  private static readonly int[] VOLUME_TABLE = new[]
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
  public ushort Counter;
  public int Polarity;
  #endregion

  public Channel()
  {
    Volume = 0xF;
    Tone = 0x0;
    Counter = 0;
    Polarity = 1;
  }

  #region Methods
  public short GenerateTone()
  {
    if (Tone == 0)
      return 0;

    Counter--;

    if (Counter == 0)
    {
      Counter = Tone;
      Polarity = -Polarity;
    }

    return (short)(VOLUME_TABLE[Volume] * Polarity);
  }

  public short GenerateNoise()
  {
    return 0; // TODO
  }
  #endregion
}
