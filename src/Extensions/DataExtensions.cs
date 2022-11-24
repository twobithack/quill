namespace Sonic.Extensions
{
  public static class DataExtensions
  {
    public static string ToHex(this byte value) => value.ToString("X2");

    public static string ToHex(this ushort word) => $"{word.HighByte().ToHex()}-{word.LowByte().ToHex()}"; 

    public static byte HighByte(this ushort word) => (byte)(word >> 8);

    public static byte LowByte(this ushort word) => (byte)word;

    public static void ExtractBytes(this ushort word, ref byte high, ref byte low)
    {
      high = word.HighByte();
      low = word.LowByte();
    }
  }
}