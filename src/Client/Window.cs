﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
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
  private readonly InputHandler _input;
  private readonly Renderer _renderer;
  private readonly Emulator _emulator;
  private readonly Thread _emulationThread;
  private readonly Configuration _configuration;
  #endregion

  public Window(Emulator emulator, Configuration config) 
    : base(GameWindowSettings.Default, CreateWindowSettings(config))
  {
    _configuration = config;
    _emulator = emulator;
    _emulationThread = new Thread(_emulator.Run) { IsBackground = true };

    _audio = new Audio(_emulator.ReadAudioBuffer, _configuration);
    _renderer = new Renderer(_emulator.ReadFramebuffer, _configuration);
    _input = new InputHandler(_emulator.UpdateInput);
  }

  #region Methods
  protected override void OnLoad()
  {
    base.OnLoad();
    _emulationThread.Start();
    _renderer.Initialize();
    _audio.Play();
  }

  protected override void OnUpdateFrame(FrameEventArgs args)
  {
    base.OnUpdateFrame(args);
    _renderer.UpdateFrame();
    _input.ReadInput(KeyboardState);
  }

  protected override void OnRenderFrame(FrameEventArgs args)
  {
    base.OnRenderFrame(args);
    _renderer.RenderFrame();
    SwapBuffers();
  }

  protected override void OnUnload()
  {
    _audio.Stop();
    _renderer.Stop();
    _emulator.Stop();
    base.OnUnload();
  }

  protected override void OnResize(ResizeEventArgs e)
  {
    base.OnResize(e);
    _renderer.ResizeViewport(FramebufferSize);
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
      Icon = LoadIcon(),
      Profile = ContextProfile.Core,
      Title = "Quill",
      Vsync = VSyncMode.On,
      WindowBorder = WindowBorder.Resizable
    };
  }

  private static WindowIcon LoadIcon()
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
      return null;

    var bytes = File.ReadAllBytes("assets/icon.rgba");
    var image = new Image(128, 128, bytes);
    return new WindowIcon(image);
  }
  #endregion
}