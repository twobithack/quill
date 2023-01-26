using System;

namespace Quill.Video.Definitions;

[Flags]
public enum Status : byte
{
  Collision = 0b_0010_0000,
  Overflow  = 0b_0100_0000,
  VBlank    = 0b_1000_0000,
  All       = 0b_1110_0000
}