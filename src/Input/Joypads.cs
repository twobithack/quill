using Quill.Input.Definitions;

namespace Quill.Input;

public class Joypads
{
  private PortA _portA;
  private PortB _portB;

  public Joypads()
  {
    _portA = PortA.None;
    _portB = PortB.None;
  }

  public byte ReadPortA() => (byte)~_portA;
  public byte ReadPortB() => (byte)~_portB;

  public void SetJoypad1State(bool up,
                              bool down,
                              bool left,
                              bool right,
                              bool fireA,
                              bool fireB)
  {
    var state = _portA & ~PortA.Joy1;
    if (up)     state |= PortA.Joy1Up;
    if (down)   state |= PortA.Joy1Down;
    if (left)   state |= PortA.Joy1Left;
    if (right)  state |= PortA.Joy1Right;
    if (fireA)  state |= PortA.Joy1FireA;
    if (fireB)  state |= PortA.Joy1FireB;
    _portA = state;
  }

  public void SetJoypad2State(bool up,
                              bool down,
                              bool left,
                              bool right,
                              bool fireA,
                              bool fireB)
  {
    var stateA = _portA & ~PortA.Joy2;
    var stateB = _portB & ~PortB.Joy2;
    if (up)     stateA |= PortA.Joy2Up;
    if (down)   stateA |= PortA.Joy2Down;
    if (left)   stateB |= PortB.Joy2Left;
    if (right)  stateB |= PortB.Joy2Right;
    if (fireA)  stateB |= PortB.Joy2FireA;
    if (fireB)  stateB |= PortB.Joy2FireB;
    _portA = stateA;
    _portB = stateB;
  }
}