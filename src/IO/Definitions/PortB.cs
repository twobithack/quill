using System;

namespace Quill.IO.Definitions;

[Flags]
public enum PortB : byte
{
  None      = 0b_0000_0000,
  Pad2Left  = 0b_0000_0001,
  Pad2Right = 0b_0000_0010,
  Pad2FireA = 0b_0000_0100,
  Pad2FireB = 0b_0000_1000,
  Reset     = 0b_0001_0000,
  CONT      = 0b_0010_0000,
  TH1       = 0b_0100_0000,
  TH2       = 0b_1000_0000
}