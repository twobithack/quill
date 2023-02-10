namespace Quill.Video.Definitions
{
  public enum DisplayMode : byte
  {
    None        = 0b_0000,
    Mode_1      = 0b_0001,
    Mode_2      = 0b_0010,
    Mode_3      = 0b_0100,
    Mode_4      = 0b_1000,
    Mode_4_224  = 0b_1011,
    Mode_4_240  = 0b_1110,
  }
}
