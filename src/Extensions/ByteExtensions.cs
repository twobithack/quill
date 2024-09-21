namespace Quill.Extensions
{
  public static class ByteExtensions
  {
    public static byte Increment(this byte value) => (byte)(value + 1);
    
    public static string ToHex(this byte value) => value.ToString("X2");
  }
}