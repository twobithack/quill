using Quill.Common.Definitions;

namespace Quill.Common;

public readonly struct InputState
{
  #region Fields
  private readonly ConsoleButtons _console;
  private readonly JoypadButtons _joypad1;
  private readonly JoypadButtons _joypad2;
  private readonly Commands _commands;
  #endregion

  public InputState(ConsoleButtons console,
                    JoypadButtons joypad1,
                    JoypadButtons joypad2,
                    Commands commands)
  {
    _console = console;
    _joypad1 = joypad1;
    _joypad2 = joypad2;
    _commands = commands;
  }

  #region Properties
  public readonly bool PausePressed => (_console & ConsoleButtons.Pause) != 0;
  public readonly bool ResetPressed => (_console & ConsoleButtons.Reset) != 0;
  public readonly bool RewindPressed => (_commands & Commands.Rewind) != 0;
  public readonly bool QuickloadPressed => (_commands & Commands.Quickload) != 0;
  public readonly bool QuicksavePressed => (_commands & Commands.Quicksave) != 0;
  #endregion

  #region Methods
  public readonly bool Joypad1Pressed(JoypadButtons button) => (_joypad1 & button) != 0;
  public readonly bool Joypad2Pressed(JoypadButtons button) => (_joypad2 & button) != 0;
  #endregion
}