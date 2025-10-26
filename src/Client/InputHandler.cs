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
    var console = ConsoleButtons.None;
    if (kb.IsKeyDown(Keys.Space))       console |= ConsoleButtons.Pause;
    if (kb.IsKeyDown(Keys.Escape))      console |= ConsoleButtons.Reset;
    
    var player1 = JoypadButtons.None;
    if (kb.IsKeyDown(Keys.W))           player1 |= JoypadButtons.Up;
    if (kb.IsKeyDown(Keys.S))           player1 |= JoypadButtons.Down;
    if (kb.IsKeyDown(Keys.A))           player1 |= JoypadButtons.Left;
    if (kb.IsKeyDown(Keys.D))           player1 |= JoypadButtons.Right;
    if (kb.IsKeyDown(Keys.F))           player1 |= JoypadButtons.FireA;
    if (kb.IsKeyDown(Keys.G))           player1 |= JoypadButtons.FireB;

    var player2 = JoypadButtons.None;
    if (kb.IsKeyDown(Keys.I))           player2 |= JoypadButtons.Up;
    if (kb.IsKeyDown(Keys.K))           player2 |= JoypadButtons.Down;
    if (kb.IsKeyDown(Keys.J))           player2 |= JoypadButtons.Left;
    if (kb.IsKeyDown(Keys.L))           player2 |= JoypadButtons.Right;
    if (kb.IsKeyDown(Keys.Semicolon))   player2 |= JoypadButtons.FireA;
    if (kb.IsKeyDown(Keys.Apostrophe))  player2 |= JoypadButtons.FireB;

    var commands = Commands.None;
    if (kb.IsKeyDown(Keys.R))           commands |= Commands.Rewind;
    if (kb.IsKeyDown(Keys.Backspace))   commands |= Commands.Quickload;
    if (kb.IsKeyDown(Keys.Enter))       commands |= Commands.Quicksave;

    var state = new InputState(console, player1, player2, commands);
    _updateInput(state);
  }
}