namespace Quill.Video.Definitions
{
  public enum DisplayMode : byte
  {
    Graphic_1   = 0b_0000,
    Text        = 0b_0001,
    Graphic_2   = 0b_0010,
    Mode_1_2    = 0b_0011,
    Multicolor  = 0b_0100,
    Mode_1_3    = 0b_0101,
    Mode_2_3    = 0b_0110,
    Mode_1_2_3  = 0b_0111,
    Mode_4a     = 0b_1000,
    Mode_4b     = 0b_1010,
    Mode_4_224  = 0b_1011,
    Mode_4c     = 0b_1100,
    Mode_4_240  = 0b_1110,
    Mode_4d     = 0b_1111
  }
}
