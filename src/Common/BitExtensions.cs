namespace Quill.Common;

unsafe public static class BitExtensions
{
  public static string ToBit(this bool value) => value ? "1" : "0";
}