namespace Quill.Z80
{
  [Flags]
  public enum Flags : byte
  {
    None      = 0b_0000_0000,
    Sign      = 0b_0000_0001,
    Zero      = 0b_0000_0010,
    X         = 0b_0000_0100,
    Halfcarry = 0b_0000_1000,
    Y         = 0b_0001_0000,
    Parity    = 0b_0010_0000,
    Negative  = 0b_0100_0000,
    Carry     = 0b_1000_0000
  }
}