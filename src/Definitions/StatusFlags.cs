namespace Sonic.Definitions
{
  [Flags]
  public enum StatusFlags
  {
    None = 0,
    Sign = 1,
    Zero = 2,
    X = 4,
    Halfcarry = 8,
    Y = 16,
    Parity = 32,
    Negative = 64,
    Carry = 128
  }
}