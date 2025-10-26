using System;

namespace Quill.Common.Definitions;

[Flags]
public enum Commands : byte
{
  None      = 0,
  Rewind    = 1 << 0,
  Quickload = 1 << 1,
  Quicksave = 1 << 2
}