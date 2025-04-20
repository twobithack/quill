using Quill.Common.Extensions;
using Quill.IO.Definitions;

namespace Quill.IO;

public sealed class Ports
{
  #region Fields
  public bool NMI;

  private bool _pauseEnabled;
  private ControlPort _control;
  private PortA _portA;
  private PortB _portB;
  #endregion

  public Ports()
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

  public void SetJoypad1State(JoypadState joypad)
  {
    _portA &= ~PortA.Joy1;

    if (joypad.Up)    _portA |= PortA.Joy1Up;
    if (joypad.Down)  _portA |= PortA.Joy1Down;
    if (joypad.Left)  _portA |= PortA.Joy1Left;
    if (joypad.Right) _portA |= PortA.Joy1Right;
    if (joypad.FireA) _portA |= PortA.Joy1FireA;
    if (joypad.FireB) _portA |= PortA.Joy1FireB;

    if (!joypad.Pause) 
      _pauseEnabled = true;
    else if (_pauseEnabled)
    {
      _pauseEnabled = false;
      NMI = true;
    }
  }

  public void SetJoypad2State(JoypadState joypad)
  {
    _portA &= ~PortA.Joy2;
    _portB &= ~PortB.Joy2;

    if (joypad.Up)    _portA |= PortA.Joy2Up;
    if (joypad.Down)  _portA |= PortA.Joy2Down;
    if (joypad.Left)  _portB |= PortB.Joy2Left;
    if (joypad.Right) _portB |= PortB.Joy2Right;
    if (joypad.FireA) _portB |= PortB.Joy2FireA;
    if (joypad.FireB) _portB |= PortB.Joy2FireB;

    if (joypad.Pause && _pauseEnabled)
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