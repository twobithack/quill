using System;

namespace Quill.Video.Definitions;

[Flags]
public enum Status : byte
{
  SpriteCollision = 0b_0010_0000,
  SpriteOverflow  = 0b_0100_0000,
  VBlank          = 0b_1000_0000,
  Flags           = 0b_1110_0000
}