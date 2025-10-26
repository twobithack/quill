using Quill.IO.Definitions;

namespace Quill.IO;

public sealed class Ports
{
  #region Fields
  public bool NMI;

  private ControlPort _control;
  private PortA _portA;
  private PortB _portB;
  private bool _pausingEnabled;
  #endregion

  public Ports()
  {
    _portA = PortA.None;
    _portB = PortB.None;
  }

  #region Properties
  private bool TH1 => !GetBit(ControlPort.TH1_Input) &&
                      !GetBit(ControlPort.TH1_Output);

  private bool TH2 => !GetBit(ControlPort.TH2_Input) &&
                      !GetBit(ControlPort.TH2_Output);
  #endregion

  #region Methods
  public byte ReadPortA() => (byte)~_portA;
  public byte ReadPortB() => (byte)~_portB;

  public void WriteControl(byte value)
  {
    _control = (ControlPort)value;
    SetBit(PortB.TH1, TH1);
    SetBit(PortB.TH2, TH2);
  }
  
  public void UpdateInput(Input state)
  {
    SetBit(PortA.Joy1Up,    state.Joypad1Pressed(Buttons.Up));
    SetBit(PortA.Joy1Down,  state.Joypad1Pressed(Buttons.Down));
    SetBit(PortA.Joy1Left,  state.Joypad1Pressed(Buttons.Left));
    SetBit(PortA.Joy1Right, state.Joypad1Pressed(Buttons.Right));
    SetBit(PortA.Joy1FireA, state.Joypad1Pressed(Buttons.FireA));
    SetBit(PortA.Joy1FireB, state.Joypad1Pressed(Buttons.FireB));
    SetBit(PortA.Joy2Up,    state.Joypad2Pressed(Buttons.Up));
    SetBit(PortA.Joy2Down,  state.Joypad2Pressed(Buttons.Down));
    SetBit(PortB.Joy2Left,  state.Joypad2Pressed(Buttons.Left));
    SetBit(PortB.Joy2Right, state.Joypad2Pressed(Buttons.Right));
    SetBit(PortB.Joy2FireA, state.Joypad2Pressed(Buttons.FireA));
    SetBit(PortB.Joy2FireB, state.Joypad2Pressed(Buttons.FireB));
    SetBit(PortB.Reset,     state.ResetPressed);

    if (!state.PausePressed)
    {
      _pausingEnabled = true;
    }
    else if (_pausingEnabled)
    {
      _pausingEnabled = false;
      NMI = true;
    }
  }

  private bool GetBit(ControlPort bit) => (_control & bit) != 0;

  private void SetBit(PortA pin, bool state) => _portA = state
                                                       ? (_portA | pin)
                                                       : (_portA & ~pin);
                                                         
  private void SetBit(PortB pin, bool state) => _portB = state
                                                       ? (_portB | pin) 
                                                       : (_portB & ~pin);
  #endregion
}