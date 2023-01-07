namespace Quill.Sound;

public sealed class PSG
{
  #region Constants
  private const int SAMPLE_RATE = 44100;
  #endregion

  #region Fields
  Channel _pulse0;
  Channel _pulse1;
  Channel _pulse2;
  Channel _noise;
  #endregion

  public PSG()
  {

  }

  #region Methods
  public void WriteData(byte value)
  {

  }

  public byte[] ReadBuffer()
  {
    var r = new System.Random();
    var b = new byte[SAMPLE_RATE];
    r.NextBytes(b);
    return b;
  }
  #endregion
}
