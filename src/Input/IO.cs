using Quill.Common;
using Quill.Input.Definitions;

namespace Quill.Input;

public sealed class IO
{
  #region Constants
  private const byte TH1_DIRECTION = 0x01;
  private const byte TH2_DIRECTION = 0x03;
  private const byte TH1_OUTPUT = 0x05;
  private const byte TH2_OUTPUT = 0x07;
  #endregion

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

  public void WriteControl(byte value)
  {
    _portB &= PortB.Joy2;

    if (value.TestBit(TH1_DIRECTION) || 
        value.TestBit(TH1_OUTPUT))
      _portB |= PortB.TH1;
      
    if (value.TestBit(TH2_DIRECTION) || 
        value.TestBit(TH2_OUTPUT))
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
  #endregion
}