namespace Quill
{
  public enum Operand 
  {
    Implied,
    Immediate,
    Indirect,

    // 8-bit registers
    A, 
    B, 
    C, 
    D, 
    E,
    F, 
    H, 
    L,
    I,
    R,

    // 16-bit registers
    AF, 
    BC, 
    DE, 
    HL, 
    IX,  
    IY, 
    SP,

    // register indirect
    BCi,
    DEi,
    HLi,

    // indexed
    IXd,
    IYd,

    // flags
    Zero, 
    NonZero,
    Carry, 
    NonCarry,
    Even,
    Odd,
    Negative,
    Positive,

    // bit indexes
    Bit0,
    Bit1,
    Bit2,
    Bit3,
    Bit4,
    Bit5,
    Bit6,
    Bit7,

    // restart addresses
    RST0,
    RST1,
    RST2,
    RST3,
    RST4,
    RST5,
    RST6,
    RST7
  } 
}