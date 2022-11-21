namespace Sonic
{
  public class Word
  {
    public byte High;
    public byte Low;

    public Word();
    public Word(byte lowByte) => Low = lowByte;

    public Word(byte highByte, byte lowByte)
    {
      High = highByte;
      Low = lowByte;
    }
    
    public byte[] Bytes => new byte[] { High, Low };
  }
}