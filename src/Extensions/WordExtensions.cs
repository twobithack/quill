using System.Runtime.CompilerServices;

namespace Quill.Extensions
{
  public unsafe static class WordExtensions
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort Increment(this ushort word) => (ushort)word++;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort Decrement(this ushort word) => (ushort)word--;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte GetLowByte(this ushort word) => (byte)word;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte GetHighByte(this ushort word) => (byte)(word >> 8);

    public static string ToHex(this ushort word) => $"{word.GetHighByte().ToHex()}-{word.GetLowByte().ToHex()}";
  }
}