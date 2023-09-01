using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Quill.Core;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Quill.UI;

public sealed class Client : Game
{
  #region Constants
  private const int FRAMEBUFFER_WIDTH = 256;
  private const int FRAMEBUFFER_HEIGHT = 240;
  private const int BOTTOM_BORDER_HEIGHT = 48;
  private const int LEFT_BORDER_WIDTH = 8;

  private const int AUDIO_SAMPLE_RATE = 44100;
  private const int MIN_AUDIO_SAMPLES = 5;

  private const int PLAYER_1 = 0;
  private const int PLAYER_2 = 1;
  #endregion

  #region Fields
  private readonly Emulator _emulator;
  private readonly Thread _emulationThread;
  private readonly Thread _pollingThread;
  private readonly GraphicsDeviceManager _graphics;
  private readonly DynamicSoundEffectInstance _sound;

  private readonly Configuration _configuration;
  private readonly string _romName;
  private readonly string _savesDirectory;

  private SpriteBatch _spriteBatch;
  private Texture2D _framebuffer;
  private Rectangle _viewport;

  private bool _running;
  private bool _savesEnabled;
  #endregion

  public Client(string romPath, Configuration config)
  {
    var rom = File.ReadAllBytes(romPath);
    _emulator = new Emulator(rom, AUDIO_SAMPLE_RATE, config.ExtraScanlines);
    _emulationThread = new Thread(_emulator.Run);
    _pollingThread = new Thread(PollAudioBuffer);
    _graphics = new GraphicsDeviceManager(this);
    _sound = new DynamicSoundEffectInstance(AUDIO_SAMPLE_RATE, 
                                            AudioChannels.Mono);
    _romName = Path.GetFileNameWithoutExtension(romPath);
    _savesDirectory = Path.Combine(Path.GetDirectoryName(romPath), "saves");
    _configuration = config;
    _running = true;
  }

  #region Properties
  private string SnapshotFilepath => Path.Combine(_savesDirectory, _romName + ".save");
  #endregion

  #region Methods
  protected override void Initialize()
  {
    Window.Title = "Quill";
    ResizeViewport();

    _framebuffer = new Texture2D(GraphicsDevice, FRAMEBUFFER_WIDTH, FRAMEBUFFER_HEIGHT);
    _emulationThread.Start();
    _pollingThread.Start();
    _sound.Play();

    base.Initialize();
  }

  protected override void LoadContent() => _spriteBatch = new SpriteBatch(GraphicsDevice);

  protected override void UnloadContent()
  {
    _emulator.Stop();
    _running = false;
  }

  protected override void Update(GameTime gameTime)
  {
    var frame = _emulator.ReadFramebuffer();
    if (frame != null)
      _framebuffer.SetData(frame);

    HandleInput();
    base.Update(gameTime);
  }

  protected override void Draw(GameTime gameTime)
  {
    GraphicsDevice.Clear(Color.Black);

    _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
    _spriteBatch.Draw(_framebuffer, _viewport, Color.White);
    _spriteBatch.End();
    
    base.Draw(gameTime);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void ResizeViewport()
  {
    _viewport = new Rectangle(0, 0, 
                              _configuration.ScaleFactor * FRAMEBUFFER_WIDTH, 
                              _configuration.ScaleFactor * FRAMEBUFFER_HEIGHT);
    _graphics.PreferredBackBufferHeight = _viewport.Height;
    _graphics.PreferredBackBufferWidth = _viewport.Width;

    if (_configuration.CropBottomBorder)
    {
      var bottomBorder = _configuration.ScaleFactor * BOTTOM_BORDER_HEIGHT;
      _graphics.PreferredBackBufferHeight -= bottomBorder;
    }

    if (_configuration.CropLeftBorder)
    {
      var leftBorder = _configuration.ScaleFactor * LEFT_BORDER_WIDTH;
      _viewport.X -= leftBorder;
      _graphics.PreferredBackBufferWidth -= leftBorder;
    }

    _graphics.ApplyChanges();
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void PollAudioBuffer()
  {
    while (_running)
    {
      if (_sound.PendingBufferCount < MIN_AUDIO_SAMPLES)
      {
        var buffer = _emulator.ReadAudioBuffer();
        if (buffer != null)
          _sound.SubmitBuffer(buffer);
      }
      Thread.Sleep(10);
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void HandleInput()
  {
    if (ReadJoypadInput(PLAYER_1))
      ReadJoypadInput(PLAYER_2);
    else
      ReadKeyboardInput();
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private bool ReadJoypadInput(int player)
  {
    var joypad = GamePad.GetState(player);
    if (!joypad.IsConnected)
      return false;

    _emulator.SetJoypadState(
      joypad: player,
      up:     joypad.IsButtonDown(Buttons.DPadUp) ||
              joypad.IsButtonDown(Buttons.LeftThumbstickUp),
      down:   joypad.IsButtonDown(Buttons.DPadDown) ||
              joypad.IsButtonDown(Buttons.LeftThumbstickDown),
      left:   joypad.IsButtonDown(Buttons.DPadLeft) ||
              joypad.IsButtonDown(Buttons.LeftThumbstickLeft),
      right:  joypad.IsButtonDown(Buttons.DPadRight) ||
              joypad.IsButtonDown(Buttons.LeftThumbstickRight),
      fireA:  joypad.IsButtonDown(Buttons.A) ||
              joypad.IsButtonDown(Buttons.Y),
      fireB:  joypad.IsButtonDown(Buttons.B) ||
              joypad.IsButtonDown(Buttons.X),
      pause:  joypad.IsButtonDown(Buttons.Start)
    );
    
    if (player == PLAYER_1)
    {
      _emulator.FastForwarding = joypad.IsButtonDown(Buttons.RightTrigger);
      _emulator.Rewinding = joypad.IsButtonDown(Buttons.LeftTrigger);
      _emulator.SetResetButtonState(joypad.IsButtonDown(Buttons.Back));
      HandleSnapshotRequest(loadRequested: joypad.IsButtonDown(Buttons.LeftShoulder),
                            saveRequested: joypad.IsButtonDown(Buttons.RightShoulder));
    }

    return true;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void ReadKeyboardInput()
  {
    var kb = Keyboard.GetState();

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
      fireA:  kb.IsKeyDown(Keys.OemSemicolon),
      fireB:  kb.IsKeyDown(Keys.OemQuotes),
      pause:  kb.IsKeyDown(Keys.Space)
    );

    _emulator.FastForwarding = kb.IsKeyDown(Keys.LeftControl);
    _emulator.Rewinding = kb.IsKeyDown(Keys.R);
    _emulator.SetResetButtonState(kb.IsKeyDown(Keys.Escape));
    HandleSnapshotRequest(loadRequested: kb.IsKeyDown(Keys.Back),
                          saveRequested: kb.IsKeyDown(Keys.Enter));
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void GenerateStatic()
  {
    var random = new Random();
    var buffer = new byte[FRAMEBUFFER_WIDTH * FRAMEBUFFER_HEIGHT * 4];
    for (int index = 0; index < buffer.Length; index += 4)
      buffer[index] = buffer[index + 1] = buffer[index + 2] = (byte)(byte.MaxValue * random.NextSingle());
    _framebuffer.SetData(buffer);
  }
  #endregion
}
