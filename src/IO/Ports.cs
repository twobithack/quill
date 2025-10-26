using System.Runtime.CompilerServices;

using Quill.Common;
using Quill.Common.Definitions;
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
  private bool TH1 => !GetPin(ControlPort.TH1_Input) &&
                      !GetPin(ControlPort.TH1_Output);

  private bool TH2 => !GetPin(ControlPort.TH2_Input) &&
                      !GetPin(ControlPort.TH2_Output);
  #endregion

  #region Methods
  public byte ReadPortA() => (byte)~_portA;
  public byte ReadPortB() => (byte)~_portB;

  public void WriteControl(byte value)
  {
    _control = (ControlPort)value;
    SetPin(PortB.TH1, TH1);
    SetPin(PortB.TH2, TH2);
  }
  
  public void UpdateInput(InputState state)
  {
    SetPin(PortA.Joy1Up,    state.Joypad1Pressed(JoypadButtons.Up));
    SetPin(PortA.Joy1Down,  state.Joypad1Pressed(JoypadButtons.Down));
    SetPin(PortA.Joy1Left,  state.Joypad1Pressed(JoypadButtons.Left));
    SetPin(PortA.Joy1Right, state.Joypad1Pressed(JoypadButtons.Right));
    SetPin(PortA.Joy1FireA, state.Joypad1Pressed(JoypadButtons.FireA));
    SetPin(PortA.Joy1FireB, state.Joypad1Pressed(JoypadButtons.FireB));
    SetPin(PortA.Joy2Up,    state.Joypad2Pressed(JoypadButtons.Up));
    SetPin(PortA.Joy2Down,  state.Joypad2Pressed(JoypadButtons.Down));
    SetPin(PortB.Joy2Left,  state.Joypad2Pressed(JoypadButtons.Left));
    SetPin(PortB.Joy2Right, state.Joypad2Pressed(JoypadButtons.Right));
    SetPin(PortB.Joy2FireA, state.Joypad2Pressed(JoypadButtons.FireA));
    SetPin(PortB.Joy2FireB, state.Joypad2Pressed(JoypadButtons.FireB));
    SetPin(PortB.Reset,     state.ResetPressed);

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

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private bool GetPin(ControlPort pin) => (_control & pin) != 0;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SetPin(PortA pin, bool state) => _portA = state
                                                       ? (_portA | pin)
                                                       : (_portA & ~pin);
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]                                         
  private void SetPin(PortB pin, bool state) => _portB = state
                                                       ? (_portB | pin) 
                                                       : (_portB & ~pin);
  #endregion
}