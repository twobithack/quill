using Quill.Common.Definitions;

namespace Quill.Common;

public struct InputState
{
  #region Fields
  private PadButtons _controlPad1;
  private PadButtons _controlPad2;
  private ConsoleButtons _console;
  private Commands _commands;
  #endregion

  #region Methods
  public void SetControlPad1State(bool up,
                                  bool down,
                                  bool left,
                                  bool right,
                                  bool fireA,
                                  bool fireB)
  {
    if (up)    _controlPad1 |= PadButtons.Up;
    if (down)  _controlPad1 |= PadButtons.Down;
    if (left)  _controlPad1 |= PadButtons.Left;
    if (right) _controlPad1 |= PadButtons.Right;
    if (fireA) _controlPad1 |= PadButtons.FireA;
    if (fireB) _controlPad1 |= PadButtons.FireB;
  }

  public void SetControlPad2State(bool up,
                                  bool down,
                                  bool left,
                                  bool right,
                                  bool fireA,
                                  bool fireB)
  {
    if (up)    _controlPad2 |= PadButtons.Up;
    if (down)  _controlPad2 |= PadButtons.Down;
    if (left)  _controlPad2 |= PadButtons.Left;
    if (right) _controlPad2 |= PadButtons.Right;
    if (fireA) _controlPad2 |= PadButtons.FireA;
    if (fireB) _controlPad2 |= PadButtons.FireB;
  }

  public void SetConsoleState(bool pause,
                              bool reset)
  {
    if (pause) _console |= ConsoleButtons.Pause;
    if (reset) _console |= ConsoleButtons.Reset;
  }

  public void SetCommandState(bool rewind,
                              bool quickload,
                              bool quicksave)
  {
    if (rewind)    _commands |= Commands.Rewind;
    if (quickload) _commands |= Commands.Quickload;
    if (quicksave) _commands |= Commands.Quicksave;
  }

  public readonly bool IsP1ButtonDown(PadButtons button) => (_controlPad1 & button) != 0;
  public readonly bool IsP2ButtonDown(PadButtons button) => (_controlPad2 & button) != 0;
  public readonly bool IsButtonDown(ConsoleButtons button) => (_console & button) != 0;
  public readonly bool IsButtonDown(Commands command) => (_commands & command) != 0;
  #endregion
}