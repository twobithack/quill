using System;
using System.IO;
using System.Threading;

using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Quill.Core;

namespace Quill.Client;

public sealed class Window : GameWindow
{
  #region Constants
  private const int FRAMEBUFFER_WIDTH = 256;
  private const int FRAMEBUFFER_HEIGHT = 240;
  private const int BOTTOM_BORDER_HEIGHT = 48;
  private const int LEFT_BORDER_WIDTH = 8;
  private const int AUDIO_SAMPLE_RATE = 44100;

  private const int PLAYER_1 = 0;
  private const int PLAYER_2 = 1;
  #endregion

  #region Fields
  private readonly Audio _audio;
  private readonly Graphics _graphics;
  private readonly Emulator _emulator;
  private readonly Thread _emulationThread;
  private readonly Configuration _config;

  private readonly string _romName;
  private readonly string _savesDirectory;
  private bool _savesEnabled;
  #endregion

  public Window(string romPath, Configuration config)
    : base(
      GameWindowSettings.Default,
      new NativeWindowSettings
      {
        Title = "Quill",
        Size = new Vector2i(
          config.ScaleFactor * (FRAMEBUFFER_WIDTH - (config.CropLeftBorder ? LEFT_BORDER_WIDTH : 0)),
          config.ScaleFactor * (FRAMEBUFFER_HEIGHT - (config.CropBottomBorder ? BOTTOM_BORDER_HEIGHT : 0)) 
        ),
        AspectRatio = (FRAMEBUFFER_WIDTH - (config.CropLeftBorder ? LEFT_BORDER_WIDTH : 0), 
                       FRAMEBUFFER_HEIGHT - (config.CropBottomBorder ? BOTTOM_BORDER_HEIGHT : 0)),
        WindowBorder = WindowBorder.Resizable,
        APIVersion = new Version(3, 3),
        Profile = ContextProfile.Core,
        Vsync = VSyncMode.On
      })
  {
    var rom = File.ReadAllBytes(romPath);
    _emulator = new Emulator(rom, AUDIO_SAMPLE_RATE, config.VirtualScanlines);
    _emulationThread = new Thread(_emulator.Run) { IsBackground = true };
    _audio = new Audio(AUDIO_SAMPLE_RATE, _emulator.ReadAudioBuffer);
    _graphics = new Graphics(config, _emulator.ReadFramebuffer);

    _romName = Path.GetFileNameWithoutExtension(romPath);
    _savesDirectory = Path.Combine(Path.GetDirectoryName(romPath), "saves");
    _config = config;
  }

  #region Properties
  private string SnapshotFilepath => Path.Combine(_savesDirectory, _romName + ".save");
  private int TextureWidth => FRAMEBUFFER_WIDTH - (_config.CropLeftBorder ? LEFT_BORDER_WIDTH : 0);
  private int TextureHeight => FRAMEBUFFER_HEIGHT - (_config.CropBottomBorder ? BOTTOM_BORDER_HEIGHT : 0);
  #endregion

  #region Methods
  protected override void OnLoad()
  {
    base.OnLoad();
    _emulationThread.Start();
    _graphics.Initialize();
    _audio.Play();
  }

  protected override void OnUpdateFrame(FrameEventArgs args)
  {
    base.OnUpdateFrame(args);
    _graphics.UpdateFrame();
    HandleInput();
  }

  protected override void OnRenderFrame(FrameEventArgs args)
  {
    base.OnRenderFrame(args);
    _graphics.RenderFrame();
    SwapBuffers();
  }

  protected override void OnUnload()
  {
    _audio.Stop();
    _graphics.Stop();
    _emulator.Stop();
    base.OnUnload();
  }

  protected override void OnResize(ResizeEventArgs e)
  {
    base.OnResize(e);

    var textureAR = (float) TextureWidth / TextureHeight;
    var viewportAR = (float) FramebufferSize.X / FramebufferSize.Y;

    int width, height, x, y;
    if (viewportAR > textureAR)
    {
      height = FramebufferSize.Y;
      width = (int)(height * textureAR);
      x = (FramebufferSize.X - width) / 2;
      y = 0;
    }
    else
    {
      width = FramebufferSize.X;
      height = (int)(width / textureAR);
      y = (FramebufferSize.Y - height) / 2;
      x = 0;
    }
    
    _graphics.ResizeViewport(x, y, width, height);
  }

  private void HandleInput() => ReadKeyboardInput();

  private void ReadKeyboardInput()
  {
    var kb = KeyboardState.GetSnapshot();

    _emulator.SetJoypadState(
      joypad: PLAYER_1,
      up:     kb.IsKeyDown(Keys.W),
      down:   kb.IsKeyDown(Keys.S),
      left:   kb.IsKeyDown(Keys.A),
      right:  kb.IsKeyDown(Keys.D),
      fireA:  kb.IsKeyDown(Keys.F),
      fireB:  kb.IsKeyDown(Keys.G),
      pause:  kb.IsKeyDown(Keys.Space)
    );

    _emulator.SetJoypadState(
      joypad: PLAYER_2,
      up:     kb.IsKeyDown(Keys.I),
      down:   kb.IsKeyDown(Keys.K),
      left:   kb.IsKeyDown(Keys.J),
      right:  kb.IsKeyDown(Keys.L),
      fireA:  kb.IsKeyDown(Keys.Semicolon),
      fireB:  kb.IsKeyDown(Keys.Apostrophe),
      pause:  kb.IsKeyDown(Keys.Space)
    );

    _emulator.Rewinding = kb.IsKeyDown(Keys.R);
    _emulator.SetResetButtonState(kb.IsKeyDown(Keys.Escape));
    HandleSnapshotRequest(loadRequested: kb.IsKeyDown(Keys.Backspace),
                          saveRequested: kb.IsKeyDown(Keys.Enter));
  }

  private void HandleSnapshotRequest(bool loadRequested,
                                     bool saveRequested)
  {
    if (!_savesEnabled)
    {
      _savesEnabled = !loadRequested && 
                      !saveRequested; 
      return;
    }

    if (loadRequested)
    {
      _emulator.LoadState(SnapshotFilepath);
      _savesEnabled = false;
    }
    else if (saveRequested)
    {
      _emulator.SaveState(SnapshotFilepath);
      _savesEnabled = false;
    }
  }
  #endregion
}
