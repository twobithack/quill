using System.Runtime.CompilerServices;

namespace Quill.Extensions;

unsafe static class WordExtensions
{
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static ushort Increment(this ushort word) => (ushort)(word + 1);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static ushort Decrement(this ushort word) => (ushort)(word - 1);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static byte LowByte(this ushort word) => (byte)word;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static byte HighByte(this ushort word) => (byte)(word >> 8);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool Sign(this ushort value) => (value & 0b_1000_0000_0000_0000) > 0;

  public static string ToHex(this ushort word) => $"{word.HighByte().ToHex()}{word.LowByte().ToHex()}";
}