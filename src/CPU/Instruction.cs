using Quill.CPU.Definitions;

namespace Quill.CPU;

public readonly struct Instruction
{
  public readonly Operation Operation;
  public readonly Operand Destination;
  public readonly Operand Source;
  public readonly byte Cycles;

  public Instruction()
    : this(Operation.NOP, Operand.Implied, Operand.Implied, 4) { }

  public Instruction(Operation op, Operand dst, Operand src, byte cycles)
  {
    Operation = op;
    Destination = dst;
    Source = src;
    Cycles = cycles;
  }

  public override string ToString() => $"{Operation} {Destination},{Source}";
}
