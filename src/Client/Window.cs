﻿using System;
using System.IO;
using System.Threading;

using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Quill.Common;
using Quill.Core;

namespace Quill.Client;

public sealed class Window : GameWindow
{
  #region Constants
  private const int FRAMEBUFFER_WIDTH = 256;
  private const int FRAMEBUFFER_HEIGHT = 240;
  private const int BOTTOM_BORDER_HEIGHT = 48;
  private const int LEFT_BORDER_WIDTH = 8;
  #endregion

  #region Fields
  private readonly Audio _audio;
  private readonly Graphics _graphics;
  private readonly Input _input;
  private readonly Emulator _emulator;
  private readonly Thread _emulationThread;
  private readonly Configuration _configuration;

  private readonly string _romName;
  private readonly string _savesDirectory;
  private bool _savesEnabled;
  #endregion

  public Window(string romPath, Configuration config) 
    : base(GameWindowSettings.Default, CreateWindowSettings(config))
  {
    var rom = File.ReadAllBytes(romPath);
    _romName = Path.GetFileNameWithoutExtension(romPath);
    _savesDirectory = Path.Combine(Path.GetDirectoryName(romPath), "saves");
    _configuration = config;

    _emulator = new Emulator(rom, _configuration);
    _emulationThread = new Thread(_emulator.Run) { IsBackground = true };

    _audio = new Audio(_emulator.ReadAudioBuffer, _configuration);
    _graphics = new Graphics(_emulator.ReadFramebuffer, _configuration);
    _input = new Input(_emulator.SetJoypadState,
                       _emulator.SetResetButtonState,
                       _emulator.SetRewinding);
  }

  private string SnapshotFilepath => Path.Combine(_savesDirectory, _romName + ".save");

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

    var (loadRequested, saveRequested) = _input.HandleInput(KeyboardState);
    HandleSnapshotRequest(loadRequested, saveRequested);
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
    _graphics.ResizeViewport(FramebufferSize);
  }

  private static NativeWindowSettings CreateWindowSettings(Configuration config)
  {
    var frameWidth = FRAMEBUFFER_WIDTH - (config.CropLeftBorder ? LEFT_BORDER_WIDTH : 0);
    var frameHeight = FRAMEBUFFER_HEIGHT - (config.CropBottomBorder ? BOTTOM_BORDER_HEIGHT : 0);
    
    var clientWidth = frameWidth * config.ScaleFactor;
    var clientHeight = frameHeight * config.ScaleFactor;

    if (config.FixAspectRatio)
      clientWidth = (int)(clientWidth * (8f / 7f));

    return new NativeWindowSettings
    {
      APIVersion = new Version(3, 3),
      AspectRatio = (clientWidth, clientHeight),
      ClientSize = new Vector2i(clientWidth, clientHeight),
      Profile = ContextProfile.Core,
      Title = "Quill",
      Vsync = VSyncMode.On,
      WindowBorder = WindowBorder.Resizable
    };
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
      Directory.CreateDirectory(_savesDirectory);
      _emulator.SaveState(SnapshotFilepath);
      _savesEnabled = false;
    }
  }
  #endregion
}