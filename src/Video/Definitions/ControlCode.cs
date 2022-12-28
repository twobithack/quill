namespace Quill.Video.Definitions;

public enum ControlCode : byte
{
  ReadVRAM      = 0b_00,
  WriteVRAM     = 0b_01,
  WriteRegister = 0b_10,
  WriteCRAM     = 0b_11
}