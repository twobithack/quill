using System;


using OpenTK.Windowing.GraphicsLibraryFramework;
using Quill.Common;

namespace Quill.Client;

public sealed class InputHandler
{
  #region Constants
  private const int GP_A            = 0;
  private const int GP_B            = 1;
  private const int GP_X            = 2;
  private const int GP_Y            = 3;
  private const int GP_LEFT_BUMPER  = 4;
  private const int GP_RIGHT_BUMPER = 5;
  private const int GP_BACK         = 6;
  private const int GP_START        = 7;
  private const int GP_GUIDE        = 8;
  private const int GP_LEFT_THUMB   = 9;
  private const int GP_RIGHT_THUMB  = 10;
  private const int GP_DPAD_UP      = 11;
  private const int GP_DPAD_RIGHT   = 12;
  private const int GP_DPAD_DOWN    = 13;
  private const int GP_DPAD_LEFT    = 14;
  private const int GP_LEFT_TRIGGER  = 4;
  private const int GP_RIGHT_TRIGGER = 5;
  private const float AXIS_DEADZONE = 0.35f;
  #endregion

  #region Fields
  private readonly Action<InputState> _updateInput;
  #endregion
  
  public InputHandler(Action<InputState> inputSetter) => _updateInput = inputSetter;

  public unsafe void ReadInput(KeyboardState kb)
  {
    var input = new InputState();

    if (GLFW.GetGamepadState(0, out var gp0))
    {
      input.SetJoypad1State(
        up:    IsButtonDown(gp0, GP_DPAD_UP)    || IsStickUp(gp0),
        down:  IsButtonDown(gp0, GP_DPAD_DOWN)  || IsStickDown(gp0),
        left:  IsButtonDown(gp0, GP_DPAD_LEFT)  || IsStickLeft(gp0),
        right: IsButtonDown(gp0, GP_DPAD_RIGHT) || IsStickRight(gp0),
        fireA: IsButtonDown(gp0, GP_X)          || IsButtonDown(gp0, GP_Y),
        fireB: IsButtonDown(gp0, GP_A)          || IsButtonDown(gp0, GP_B)
      );
      
      input.SetConsoleState(
        pause: IsButtonDown(gp0, GP_START),
        reset: IsButtonDown(gp0, GP_BACK)
      );

      input.SetCommandState(
        rewind:    IsButtonDown(gp0, GP_LEFT_BUMPER),
        quickload: IsTriggerDown(gp0, GP_LEFT_TRIGGER),
        quicksave: IsTriggerDown(gp0, GP_RIGHT_TRIGGER)
      );
    }
    else
    {
      input.SetJoypad1State(
        up:    kb.IsKeyDown(Keys.W),
        down:  kb.IsKeyDown(Keys.S),
        left:  kb.IsKeyDown(Keys.A),
        right: kb.IsKeyDown(Keys.D),
        fireA: kb.IsKeyDown(Keys.F),
        fireB: kb.IsKeyDown(Keys.G)
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
    }

    if (GLFW.GetGamepadState(1, out var gp1))
    {
      input.SetJoypad2State(
        up:    IsButtonDown(gp1, GP_DPAD_UP)    || IsStickUp(gp1),
        down:  IsButtonDown(gp1, GP_DPAD_DOWN)  || IsStickDown(gp1),
        left:  IsButtonDown(gp1, GP_DPAD_LEFT)  || IsStickLeft(gp1),
        right: IsButtonDown(gp1, GP_DPAD_RIGHT) || IsStickRight(gp1),
        fireA: IsButtonDown(gp1, GP_X)          || IsButtonDown(gp1, GP_Y),
        fireB: IsButtonDown(gp1, GP_A)          || IsButtonDown(gp1, GP_B)
      );
    }
    else
    {
      input.SetJoypad2State(
        up:    kb.IsKeyDown(Keys.I),
        down:  kb.IsKeyDown(Keys.K),
        left:  kb.IsKeyDown(Keys.J),
        right: kb.IsKeyDown(Keys.L),
        fireA: kb.IsKeyDown(Keys.Semicolon),
        fireB: kb.IsKeyDown(Keys.Apostrophe)
      );
    }

    _updateInput(input);
  }

  private static unsafe bool IsButtonDown(in GamepadState pad, int button) => pad.Buttons[button] == (byte)InputAction.Press;
  private static unsafe bool IsTriggerDown(in GamepadState pad, int trigger) => pad.Axes[trigger] > AXIS_DEADZONE;
  private static unsafe bool IsStickUp(in GamepadState pad)    => pad.Axes[1] < -AXIS_DEADZONE;
  private static unsafe bool IsStickDown(in GamepadState pad)  => pad.Axes[1] >  AXIS_DEADZONE;
  private static unsafe bool IsStickLeft(in GamepadState pad)  => pad.Axes[0] < -AXIS_DEADZONE;
  private static unsafe bool IsStickRight(in GamepadState pad) => pad.Axes[0] >  AXIS_DEADZONE;
}