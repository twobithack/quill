using System;

using OpenTK.Windowing.GraphicsLibraryFramework;
using Quill.IO;
using Quill.IO.Definitions;

namespace Quill.Client;

public sealed class InputHandler
{
  #region Fields
  private readonly Action<Input> _setInput;
  #endregion
  
  public InputHandler(Action<Input> inputSetter) => _setInput = inputSetter;

  public void ReadInput(KeyboardState kb)
  {
    var state = new Input();

    if (kb.IsKeyDown(Keys.W))           state.Joypad1 |= Buttons.Up;
    if (kb.IsKeyDown(Keys.S))           state.Joypad1 |= Buttons.Down;
    if (kb.IsKeyDown(Keys.A))           state.Joypad1 |= Buttons.Left;
    if (kb.IsKeyDown(Keys.D))           state.Joypad1 |= Buttons.Right;
    if (kb.IsKeyDown(Keys.F))           state.Joypad1 |= Buttons.FireA;
    if (kb.IsKeyDown(Keys.G))           state.Joypad1 |= Buttons.FireB;
    if (kb.IsKeyDown(Keys.I))           state.Joypad2 |= Buttons.Up;
    if (kb.IsKeyDown(Keys.K))           state.Joypad2 |= Buttons.Down;
    if (kb.IsKeyDown(Keys.J))           state.Joypad2 |= Buttons.Left;
    if (kb.IsKeyDown(Keys.L))           state.Joypad2 |= Buttons.Right;
    if (kb.IsKeyDown(Keys.Semicolon))   state.Joypad2 |= Buttons.FireA;
    if (kb.IsKeyDown(Keys.Apostrophe))  state.Joypad2 |= Buttons.FireB;

    state.PausePressed = kb.IsKeyDown(Keys.Space);
    state.ResetPressed = kb.IsKeyDown(Keys.Escape);
    state.RewindPressed = kb.IsKeyDown(Keys.R);
    state.QuickloadPressed = kb.IsKeyDown(Keys.Backspace);
    state.QuicksavePressed = kb.IsKeyDown(Keys.Enter);
    
    _setInput(state);
  }
}