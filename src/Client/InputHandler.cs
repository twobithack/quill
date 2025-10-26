using System;

using OpenTK.Windowing.GraphicsLibraryFramework;
using Quill.Common;
using Quill.Common.Definitions;

namespace Quill.Client;

public sealed class InputHandler
{
  #region Fields
  private readonly Action<InputState> _updateInput;
  #endregion
  
  public InputHandler(Action<InputState> inputSetter) => _updateInput = inputSetter;

  public void ReadInput(KeyboardState kb)
  {
    var input = new InputState();

    input.SetJoypad1State(
      up:    kb.IsKeyDown(Keys.W),
      down:  kb.IsKeyDown(Keys.S),
      left:  kb.IsKeyDown(Keys.A),
      right: kb.IsKeyDown(Keys.D),
      fireA: kb.IsKeyDown(Keys.F),
      fireB: kb.IsKeyDown(Keys.G)
    );
    
    input.SetJoypad2State(
      up:    kb.IsKeyDown(Keys.I),
      down:  kb.IsKeyDown(Keys.K),
      left:  kb.IsKeyDown(Keys.J),
      right: kb.IsKeyDown(Keys.L),
      fireA: kb.IsKeyDown(Keys.Semicolon),
      fireB: kb.IsKeyDown(Keys.Apostrophe)
    );

    input.SetConsoleState(
      pause: kb.IsKeyDown(Keys.Space),
      reset: kb.IsKeyDown(Keys.Escape)
    );

    input.SetCommandState(
      rewind:    kb.IsKeyDown(Keys.R),
      quickload: kb.IsKeyDown(Keys.Backspace),
      quicksave: kb.IsKeyDown(Keys.Enter)
    );

    _updateInput(input);
  }
}