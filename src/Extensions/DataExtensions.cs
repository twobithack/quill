namespace Sonic.Extensions
{
  public static class DataExtensions
  {
    public static string ToHex(this byte value) => value.ToString("X2");

    public static string ToHex(this ushort word) => $"{word.GetHighByte().ToHex()}-{word.GetLowByte().ToHex()}"; 

    private static byte GetHighByte(this ushort word) => (byte)(word >> 8);

    private static byte GetLowByte(this ushort word) => (byte)word;

    public static void ExtractBytes(this ushort word, ref byte high, ref byte low)
    {
      high = word.GetHighByte();
      low = word.GetLowByte();
    }
  }
}