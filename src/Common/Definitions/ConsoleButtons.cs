using System;

namespace Quill.Common.Definitions;

[Flags]
public enum ConsoleButtons : byte
{
  None  = 0,
  Pause = 1 << 0,
  Reset = 1 << 1
}