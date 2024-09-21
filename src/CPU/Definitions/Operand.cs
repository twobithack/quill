namespace Quill.CPU.Definitions;

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
  IXl,
  IXh,
  IYl,
  IYh,

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
  Positive
} 