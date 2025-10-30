using Quill.Common.Definitions;

namespace Quill.Common;

public struct InputState
{
  #region Fields
  private JoypadButtons _joypad1;
  private JoypadButtons _joypad2;
  private ConsoleButtons _console;
  private Commands _commands;
  #endregion

  #region Methods
  public void SetJoypad1State(bool up,
                              bool down,
                              bool left,
                              bool right,
                              bool fireA,
                              bool fireB)
  {
    if (up)    _joypad1 |= JoypadButtons.Up;
    if (down)  _joypad1 |= JoypadButtons.Down;
    if (left)  _joypad1 |= JoypadButtons.Left;
    if (right) _joypad1 |= JoypadButtons.Right;
    if (fireA) _joypad1 |= JoypadButtons.FireA;
    if (fireB) _joypad1 |= JoypadButtons.FireB;
  }
  
  public void SetJoypad2State(bool up,
                              bool down,
                              bool left,
                              bool right,
                              bool fireA,
                              bool fireB)
  {
    if (up)    _joypad2 |= JoypadButtons.Up;
    if (down)  _joypad2 |= JoypadButtons.Down;
    if (left)  _joypad2 |= JoypadButtons.Left;
    if (right) _joypad2 |= JoypadButtons.Right;
    if (fireA) _joypad2 |= JoypadButtons.FireA;
    if (fireB) _joypad2 |= JoypadButtons.FireB;
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

  public readonly bool IsJ1ButtonDown(JoypadButtons button) => (_joypad1 & button) != 0;
  public readonly bool IsJ2ButtonDown(JoypadButtons button) => (_joypad2 & button) != 0;
  public readonly bool IsButtonDown(ConsoleButtons button) => (_console & button) != 0;
  public readonly bool IsButtonDown(Commands command) => (_commands & command) != 0;
  #endregion
}