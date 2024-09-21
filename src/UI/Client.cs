using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Quill.Core;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Quill.UI;

public sealed class Client : Game
{
  #region Constants
  private const int AUDIO_SAMPLE_RATE = 44100;
  private const int MIN_AUDIO_SAMPLES = 5;
  private const int FRAMEBUFFER_WIDTH = 256;
  private const int FRAMEBUFFER_HEIGHT = 192;
  private const int BORDER_MASK_WIDTH = 8;
  private const int PLAYER_1 = 0;
  private const int PLAYER_2 = 1;
  #endregion

  #region Fields
  private readonly Emulator _emulator;
  private readonly Thread _emulationThread;
  private readonly Thread _bufferingThread;
  private readonly GraphicsDeviceManager _graphics;
  private readonly DynamicSoundEffectInstance _sound;
  private readonly string _romName;
  private readonly string _saveDirectory;
  private readonly bool _cropBorder;
  private readonly int _scale;
  private SpriteBatch _spriteBatch;
  private Texture2D _framebuffer;
  private Rectangle _viewport;
  private bool _savesEnabled;
  private bool _running;
  #endregion

  public Client(string filepath,
                int scaleFactor = 1,
                int extraScanlines = 0,
                bool cropBorders = true)
  {
    var rom = File.ReadAllBytes(filepath);
    _emulator = new Emulator(rom, extraScanlines);
    _emulationThread = new Thread(_emulator.Run);
    _bufferingThread = new Thread(UpdateAudioBuffer);
    _graphics = new GraphicsDeviceManager(this);
    _sound = new DynamicSoundEffectInstance(AUDIO_SAMPLE_RATE, 
                                            AudioChannels.Mono);
    _romName = Path.GetFileNameWithoutExtension(filepath);
    _saveDirectory = Path.GetDirectoryName(filepath);
    _cropBorder = cropBorders;
    _scale = scaleFactor;
    _running = true;
  }

  #region Properties
  private string SnapshotFilepath => Path.Combine(_saveDirectory, _romName + ".save");
  private int ViewportHeight => _scale * FRAMEBUFFER_HEIGHT;
  private int ViewportWidth => _scale * FRAMEBUFFER_WIDTH;
  #endregion

  #region Methods
  protected override void Initialize()
  {
    Window.Title = "Quill";
    _framebuffer = new Texture2D(GraphicsDevice, FRAMEBUFFER_WIDTH, FRAMEBUFFER_HEIGHT);
    _graphics.PreferredBackBufferHeight = ViewportHeight;
    _graphics.PreferredBackBufferWidth = ViewportWidth;
    _graphics.ApplyChanges();
    ResizeViewport();

    _emulationThread.Start();
    _bufferingThread.Start();
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
    HandleInput();
    base.Update(gameTime);
  }

  protected override void Draw(GameTime gameTime)
  {
    var frame = _emulator.ReadFramebuffer();
    if (frame == null) 
      return;
   
    GraphicsDevice.Clear(Color.Black);
    _framebuffer.SetData(frame);
    _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
    _spriteBatch.Draw(_framebuffer, _viewport, Color.White);
    _spriteBatch.End();
    
    base.Draw(gameTime);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void ResizeViewport()
  {
    _viewport = new Rectangle(0, 0, ViewportWidth, ViewportHeight);
    if (_cropBorder)
    {
      var offset = _scale * BORDER_MASK_WIDTH;
      _viewport.X -= offset;
      _graphics.PreferredBackBufferWidth -= offset;
      _graphics.ApplyChanges();    
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void UpdateAudioBuffer()
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
      pause:  kb.IsKeyDown(Keys.LeftControl)
    );

    _emulator.SetJoypadState(
      joypad: PLAYER_2,
      up:     kb.IsKeyDown(Keys.Up),
      down:   kb.IsKeyDown(Keys.Down),
      left:   kb.IsKeyDown(Keys.Left),
      right:  kb.IsKeyDown(Keys.Right),
      fireA:  kb.IsKeyDown(Keys.OemComma),
      fireB:  kb.IsKeyDown(Keys.OemPeriod),
      pause:  kb.IsKeyDown(Keys.Space)
    );

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
  #endregion
}
