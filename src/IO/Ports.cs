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
    if (input.IsP1ButtonDown(PadButtons.Up))      SetPin(PortA.Pad1Up);
    if (input.IsP1ButtonDown(PadButtons.Down))    SetPin(PortA.Pad1Down);
    if (input.IsP1ButtonDown(PadButtons.Left))    SetPin(PortA.Pad1Left);
    if (input.IsP1ButtonDown(PadButtons.Right))   SetPin(PortA.Pad1Right);
    if (input.IsP1ButtonDown(PadButtons.FireA))   SetPin(PortA.Pad1FireA);
    if (input.IsP1ButtonDown(PadButtons.FireB))   SetPin(PortA.Pad1FireB);

    if (input.IsP2ButtonDown(PadButtons.Up))      SetPin(PortA.Pad2Up);
    if (input.IsP2ButtonDown(PadButtons.Down))    SetPin(PortA.Pad2Down);
    if (input.IsP2ButtonDown(PadButtons.Left))    SetPin(PortB.Pad2Left);
    if (input.IsP2ButtonDown(PadButtons.Right))   SetPin(PortB.Pad2Right);
    if (input.IsP2ButtonDown(PadButtons.FireA))   SetPin(PortB.Pad2FireA);
    if (input.IsP2ButtonDown(PadButtons.FireB))   SetPin(PortB.Pad2FireB);

    if (input.IsButtonDown(ConsoleButtons.Reset)) SetPin(PortB.Reset);

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
  private void SetPin(PortA pin) => _portA |= pin;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SetPin(PortB pin) => _portB |= pin;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SetPin(PortB pin, bool state) => _portB = state
                                                       ? (_portB | pin)
                                                       : (_portB & ~pin);
  #endregion
}