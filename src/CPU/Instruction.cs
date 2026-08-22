using Quill.CPU.Definitions;

namespace Quill.CPU;

public readonly struct Instruction
{
  public readonly Operation Operation;
  public readonly Operand Destination;
  public readonly Operand Source;
  public readonly byte? Parameter;
  public readonly byte TStates;

  public Instruction()
    : this(Operation.NOP, Operand.Implied, Operand.Implied, 4) { }

  public Instruction(Operation op, Operand dst, Operand src, byte tstates)
    : this(op, dst, src, null, tstates) { }

  public Instruction(Operation op, Operand dst, Operand src, byte? param, byte tstates)
  {
    Operation = op;
    Destination = dst;
    Source = src;
    Parameter = param;
    TStates = tstates;
  }

  public override string ToString() => Parameter.HasValue
                                     ? $"{Operation} {Destination},{Source} ({Parameter.Value})"
                                     : $"{Operation} {Destination},{Source}";
}
