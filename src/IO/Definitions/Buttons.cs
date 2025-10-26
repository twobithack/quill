using System;

namespace Quill.IO.Definitions;

[Flags]
public enum Buttons : byte
{
  None  = 0b_0000_0000,
  Up    = 0b_0000_0001,
  Down  = 0b_0000_0010,
  Left  = 0b_0000_0100,
  Right = 0b_0000_1000,
  FireA = 0b_0001_0000,
  FireB = 0b_0010_0000
}