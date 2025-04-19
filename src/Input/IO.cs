using Quill.Common.Extensions;
using Quill.Input.Definitions;

namespace Quill.Input;

public sealed class IO
{
  #region Fields
  public bool NMI;

  private bool _pauseEnabled;
  private ControlPort _control;
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

  public void WriteControl(byte value)
  {
    _control = (ControlPort)value;
    _portB &= PortB.Joy2;

    if (!GetControlFlag(ControlPort.TH1_Input) &&
        !GetControlFlag(ControlPort.TH1_Output))
      _portB |= PortB.TH1;
      
    if (!GetControlFlag(ControlPort.TH2_Input) &&
        !GetControlFlag(ControlPort.TH2_Output))
      _portB |= PortB.TH2;
  }

  public void SetJoypad1State(bool up,
                              bool down,
                              bool left,
                              bool right,
                              bool fireA,
                              bool fireB,
                              bool pause)
  {
    _portA &= ~PortA.Joy1;

    if (up)     _portA |= PortA.Joy1Up;
    if (down)   _portA |= PortA.Joy1Down;
    if (left)   _portA |= PortA.Joy1Left;
    if (right)  _portA |= PortA.Joy1Right;
    if (fireA)  _portA |= PortA.Joy1FireA;
    if (fireB)  _portA |= PortA.Joy1FireB;

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
    _portA &= ~PortA.Joy2;
    _portB &= ~PortB.Joy2;

    if (up)     _portA |= PortA.Joy2Up;
    if (down)   _portA |= PortA.Joy2Down;
    if (left)   _portB |= PortB.Joy2Left;
    if (right)  _portB |= PortB.Joy2Right;
    if (fireA)  _portB |= PortB.Joy2FireA;
    if (fireB)  _portB |= PortB.Joy2FireB;

    if (pause && _pauseEnabled)
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

  private bool GetControlFlag(ControlPort flag) => (_control & flag) != 0;
  #endregion
}