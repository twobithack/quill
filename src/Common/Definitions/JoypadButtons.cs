using System;

namespace Quill.Common.Definitions;

[Flags]
public enum JoypadButtons : byte
{
  None  = 0,
  Up    = 1 << 0,
  Down  = 1 << 1,
  Left  = 1 << 2,
  Right = 1 << 3,
  FireA = 1 << 4,
  FireB = 1 << 5
}