using System;

namespace Quill.CPU.Definitions;

[Flags]
public enum Flags : byte
{
  None      = 0b_0000_0000,
  Carry     = 0b_0000_0001,
  Negative  = 0b_0000_0010,
  Parity    = 0b_0000_0100,
  X         = 0b_0000_1000,
  Halfcarry = 0b_0001_0000,
  Y         = 0b_0010_0000,
  Zero      = 0b_0100_0000,
  Sign      = 0b_1000_0000
} 