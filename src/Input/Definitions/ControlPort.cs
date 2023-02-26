using System;

namespace Quill.Input.Definitions;

[Flags]
public enum ControlPort : byte
{
  None        = 0b_0000_0000, 
  TR1_Input   = 0b_0000_0001,
  TH1_Input   = 0b_0000_0010,
  TR2_Input   = 0b_0000_0100,
  TH2_Input   = 0b_0000_1000,
  TR1_Output  = 0b_0001_0000,
  TH1_Output  = 0b_0010_0000,
  TR2_Output  = 0b_0100_0000,
  TH2_Output  = 0b_1000_0000,
  All         = 0b_1111_1111
}