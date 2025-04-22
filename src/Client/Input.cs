using System;

using OpenTK.Windowing.GraphicsLibraryFramework;
using Quill.IO;

namespace Quill.Client;

public sealed class Input
{
  #region Constants
  private const int PLAYER_1 = 0;
  private const int PLAYER_2 = 1;
  #endregion

  #region Fields
  private readonly Action<int, JoypadState> _updateJoypad;
  private readonly Action<bool> _updateResetButton;
  private readonly Action<bool> _updateRewinding;
  #endregion
  
  public Input(Action<int, JoypadState> setJoypadState,
               Action<bool> setResetButtonState,
               Action<bool> setRewinding)
  {
    _updateJoypad = setJoypadState;
    _updateResetButton = setResetButtonState;
    _updateRewinding = setRewinding;
  }

  public (bool loadRequested, bool saveRequested) HandleInput(KeyboardState kb)
  {
    _updateJoypad(
      PLAYER_1,
      new JoypadState
      {
        Up    = kb.IsKeyDown(Keys.W),
        Down  = kb.IsKeyDown(Keys.S),
        Left  = kb.IsKeyDown(Keys.A),
        Right = kb.IsKeyDown(Keys.D),
        FireA = kb.IsKeyDown(Keys.F),
        FireB = kb.IsKeyDown(Keys.G),
        Pause = kb.IsKeyDown(Keys.Space)
      });

    _updateJoypad(
      PLAYER_2,
      new JoypadState
      {
        Up    = kb.IsKeyDown(Keys.I),
        Down  = kb.IsKeyDown(Keys.K),
        Left  = kb.IsKeyDown(Keys.J),
        Right = kb.IsKeyDown(Keys.L),
        FireA = kb.IsKeyDown(Keys.Semicolon),
        FireB = kb.IsKeyDown(Keys.Apostrophe),
        Pause = kb.IsKeyDown(Keys.Space)
      });

    _updateResetButton(kb.IsKeyDown(Keys.Escape));
    _updateRewinding(kb.IsKeyDown(Keys.R));
    return (kb.IsKeyDown(Keys.Backspace),
            kb.IsKeyDown(Keys.Enter));
  }
}