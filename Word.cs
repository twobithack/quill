namespace Sonic
{
  public class Word
  {
    public byte High = 0x00;
    public byte Low = 0x00;

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