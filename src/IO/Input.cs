using Quill.IO.Definitions;

namespace Quill.IO;

public struct Input
{
  #region Fields
  public Buttons Joypad1;
  public Buttons Joypad2;
  public bool PausePressed;
  public bool ResetPressed;
  public bool RewindPressed;
  public bool QuickloadPressed;
  public bool QuicksavePressed;
  #endregion

  #region Methods
  public readonly bool Joypad1Pressed(Buttons button) => (Joypad1 & button) != 0;
  public readonly bool Joypad2Pressed(Buttons button) => (Joypad2 & button) != 0;
  #endregion
}