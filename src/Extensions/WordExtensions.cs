namespace Quill.Extensions
{
  public static class WordExtensions
  {
    public static ushort Increment(this ushort word) => (ushort)(word + 1);

    public static bool GetBit(this ushort value, int i) => ((value >> i) & 1) != 0;

    public static bool GetLSB(this ushort value) => value.GetBit(0);

    public static bool GetMSB(this ushort value) => value.GetBit(15);

    public static byte GetLowByte(this ushort word) => (byte)word;

    public static byte GetHighByte(this ushort word) => (byte)(word >> 8);

    public static string ToHex(this ushort word) => $"{word.GetHighByte().ToHex()}-{word.GetLowByte().ToHex()}";
  }
}