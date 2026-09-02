using System;

namespace Quill.IO.Definitions;

[Flags]
public enum PortA : byte
{
  None      = 0b_0000_0000,
  Pad1Up    = 0b_0000_0001,
  Pad1Down  = 0b_0000_0010,
  Pad1Left  = 0b_0000_0100,
  Pad1Right = 0b_0000_1000,
  Pad1FireA = 0b_0001_0000,
  Pad1FireB = 0b_0010_0000,
  Pad2Up    = 0b_0100_0000,
  Pad2Down  = 0b_1000_0000
}