namespace Quill
{
  public static class Util
  {
    public static ushort ConcatBytes(byte msb, byte lsb) => (ushort)((msb << 8) + lsb);
  }
}