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
  
  public void UpdateInput(InputState input)
  {
    SetPin(PortA.Joy1Up,    input.IsJ1ButtonDown(JoypadButtons.Up));
    SetPin(PortA.Joy1Down,  input.IsJ1ButtonDown(JoypadButtons.Down));
    SetPin(PortA.Joy1Left,  input.IsJ1ButtonDown(JoypadButtons.Left));
    SetPin(PortA.Joy1Right, input.IsJ1ButtonDown(JoypadButtons.Right));
    SetPin(PortA.Joy1FireA, input.IsJ1ButtonDown(JoypadButtons.FireA));
    SetPin(PortA.Joy1FireB, input.IsJ1ButtonDown(JoypadButtons.FireB));

    SetPin(PortA.Joy2Up,    input.IsJ2ButtonDown(JoypadButtons.Up));
    SetPin(PortA.Joy2Down,  input.IsJ2ButtonDown(JoypadButtons.Down));
    SetPin(PortB.Joy2Left,  input.IsJ2ButtonDown(JoypadButtons.Left));
    SetPin(PortB.Joy2Right, input.IsJ2ButtonDown(JoypadButtons.Right));
    SetPin(PortB.Joy2FireA, input.IsJ2ButtonDown(JoypadButtons.FireA));
    SetPin(PortB.Joy2FireB, input.IsJ2ButtonDown(JoypadButtons.FireB));
    
    SetPin(PortB.Reset,     input.IsButtonDown(ConsoleButtons.Reset));

    if (!input.IsButtonDown(ConsoleButtons.Pause))
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