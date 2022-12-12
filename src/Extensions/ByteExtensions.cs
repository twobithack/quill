using System.Runtime.CompilerServices;

namespace Quill.Extensions
{
  public static class ByteExtensions
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort Concat(this byte msb, byte lsb) => (ushort)((msb << 8) + lsb);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte Increment(this byte value) => (byte)value++;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte Decrement(this byte value) => (byte)value--;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte GetLowNibble(this byte value) => (byte)(value & 0b0000_1111);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte GetHighNibble(this byte value) => (byte)(value >> 4);
    
    public static string ToHex(this byte value) => value.ToString("X2");
  }
}