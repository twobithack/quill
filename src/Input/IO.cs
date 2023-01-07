using Quill.Input.Definitions;

namespace Quill.Input;

public class IO
{
  #region Fields
  public bool NMI;

  private bool _pauseEnabled;
  private PortA _portA;
  private PortB _portB;
  #endregion

  public IO()
  {
    _portA = PortA.None;
    _portB = PortB.None;
  }

  #region Methods
  public byte ReadPortA() => (byte)~_portA;
  public byte ReadPortB() => (byte)~_portB;

  public void SetJoypad1State(bool up,
                              bool down,
                              bool left,
                              bool right,
                              bool fireA,
                              bool fireB,
                              bool pause)
  {
    
    var state = _portA & ~PortA.Joy1;
    if (up)     state |= PortA.Joy1Up;
    if (down)   state |= PortA.Joy1Down;
    if (left)   state |= PortA.Joy1Left;
    if (right)  state |= PortA.Joy1Right;
    if (fireA)  state |= PortA.Joy1FireA;
    if (fireB)  state |= PortA.Joy1FireB;

    _portA = state;

    if (!pause) 
      _pauseEnabled = true;
    else if (_pauseEnabled)
    {
      _pauseEnabled = false;
      NMI = true;
    }
  }

  public void SetJoypad2State(bool up,
                              bool down,
                              bool left,
                              bool right,
                              bool fireA,
                              bool fireB,
                              bool pause)
  {
    var stateA = _portA & ~PortA.Joy2;
    var stateB = _portB & ~PortB.Joy2;
    if (up)     stateA |= PortA.Joy2Up;
    if (down)   stateA |= PortA.Joy2Down;
    if (left)   stateB |= PortB.Joy2Left;
    if (right)  stateB |= PortB.Joy2Right;
    if (fireA)  stateB |= PortB.Joy2FireA;
    if (fireB)  stateB |= PortB.Joy2FireB;
    if (pause)  NMI = true;

    _portA = stateA;
    _portB = stateB;

    if (!pause)
      _pauseEnabled = true;
    else if (_pauseEnabled)
    {
      _pauseEnabled = false;
      NMI = true;
    }
  }

  public void SetResetButtonState(bool reset)
  {
    _portB = reset 
           ? (_portB | PortB.Reset) 
           : (_portB & ~PortB.Reset);
  }
  #endregion
}