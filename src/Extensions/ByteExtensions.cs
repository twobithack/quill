using Quill.Definitions;
using System.Runtime.CompilerServices;

namespace Quill.Extensions
{
  public unsafe static class ByteExtensions
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort Concat(this byte msb, byte lsb) => (ushort)((msb << 8) + lsb);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte Increment(this byte value) => (byte)value++;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte Decrement(this byte value) => (byte)value--;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte GetLowNibble(this byte value) => (byte)(value & 0b_0000_1111);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte GetHighNibble(this byte value) => (byte)(value >> 4);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetLSB(this byte value) => (value & 1) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetMSB(this byte value) => value.TestBit(7);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TestBit(this byte value, byte index) => ((value >> index) & 1) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte SetBit(this byte value, byte index) => (byte)(value | (1 << index));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ResetBit(this byte value, byte index) => (byte)(value & ~(1 << index));
    
    public static string ToHex(this byte value) => value.ToString("X2");
  }
}

