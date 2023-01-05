using System;

namespace Quill.Video.Definitions;

[Flags]
public enum Status : byte
{
  None      = 0b_0000_0000,
  Collision = 0b_0010_0000,
  Overflow  = 0b_0100_0000,
  VSync     = 0b_1000_0000,
}