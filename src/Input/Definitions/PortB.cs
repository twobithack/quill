using System;

namespace Quill.Input.Definitions;

[Flags]
public enum PortB : byte
{
  None      = 0b_0000_0000, 
  Joy2Left  = 0b_0000_0001,
  Joy2Right = 0b_0000_0010,
  Joy2FireA = 0b_0000_0100,
  Joy2FireB = 0b_0000_1000,
  Reset     = 0b_0001_0000,
  CONT      = 0b_0010_0000,
  TH1       = 0b_0100_0000,
  TH2       = 0b_1000_0000,
  All       = 0b_1111_1111,
  Joy2      = Joy2Left | Joy2Right | Joy2FireA | Joy2FireB
}