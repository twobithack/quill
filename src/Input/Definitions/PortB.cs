using System;

namespace Quill.Input.Definitions;

[FlagsAttribute]
public enum PortB : byte
{
  None      = 0b_0000_0000, 
  Joy2Left  = 0b_0000_0001,
  Joy2Right = 0b_0000_0010,
  Joy2FireA = 0b_0000_0100,
  Joy2FireB = 0b_0000_1000,
  Reset     = 0b_0001_0000,
  Joy2      = Joy2Left | Joy2Right | Joy2FireA | Joy2FireB,
  All       = 0b_1111_1111
}