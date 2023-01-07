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
  private const int FRAMEBUFFER_WIDTH = 256;
  private const int FRAMEBUFFER_HEIGHT = 192;
  private const int BORDER_MASK_WIDTH = 8;
  private const int PLAYER_1 = 0;
  private const int PLAYER_2 = 1;
  #endregion

  #region Fields
  private readonly Emulator _emulator;
  private readonly Thread _emulationThread;
  private readonly DynamicSoundEffectInstance _audio;
  private readonly GraphicsDeviceManager _graphics;
  private readonly string _romName;
  private readonly string _saveDirectory;
  private readonly bool _cropBorder;
  private readonly int _scale;
  private SpriteBatch _spriteBatch;
  private Texture2D _framebuffer;
  private Rectangle _viewport;
  private bool _savesEnabled;
  #endregion

  public Client(byte[] rom, 
                string romName,
                string saveDir,
                int scaleFactor = 1,
                int fakeScanlines = 0,
                bool cropBorders = true)
  {
    Content.RootDirectory = "content";
    _emulator = new Emulator(rom, fakeScanlines);
    _emulationThread = new Thread(_emulator.Run);
    _audio = new DynamicSoundEffectInstance(AUDIO_SAMPLE_RATE, 
                                            AudioChannels.Stereo);
    _graphics = new GraphicsDeviceManager(this);
    _romName = romName;
    _saveDirectory = saveDir;
    _cropBorder = cropBorders;
    _scale = scaleFactor;
  }

  #region Properties
  private string SnapshotFilepath => Path.Combine(_saveDirectory, _romName + ".save");
  #endregion

  #region Methods
  protected override void Initialize()
  {
    Window.Title = "Quill";
    _framebuffer = new Texture2D(GraphicsDevice, FRAMEBUFFER_WIDTH, FRAMEBUFFER_HEIGHT);
    _graphics.PreferredBackBufferHeight = _scale * FRAMEBUFFER_HEIGHT;
    _graphics.PreferredBackBufferWidth = _scale * FRAMEBUFFER_WIDTH;
    _graphics.ApplyChanges();    

    _viewport = GraphicsDevice.Viewport.Bounds;
    if (_cropBorder)
    {
      var offset = _scale * BORDER_MASK_WIDTH;
      _viewport.X -= offset;
      _graphics.PreferredBackBufferWidth -= offset;
      _graphics.ApplyChanges();    
    }

    _audio.Play();
    _emulationThread.Start();

    base.Initialize();
  }

  protected override void LoadContent() => _spriteBatch = new SpriteBatch(GraphicsDevice);

  protected override void UnloadContent() => _emulator.Stop();

  protected override void Update(GameTime gameTime)
  {
    ReadInput();
    ReadAudioBuffer();
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
  private void ReadAudioBuffer()
  {
    var audio = _emulator.ReadAudioBuffer();
    if (audio != null)
      _audio.SubmitBuffer(audio);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void ReadInput()
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
      pause:  joypad.IsButtonDown(Buttons.Start) ||
              joypad.IsButtonDown(Buttons.Back)
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
      up:     kb.IsKeyDown(Keys.Up) ||
              kb.IsKeyDown(Keys.W),
      down:   kb.IsKeyDown(Keys.Down) ||
              kb.IsKeyDown(Keys.S),
      left:   kb.IsKeyDown(Keys.Left) ||
              kb.IsKeyDown(Keys.A),
      right:  kb.IsKeyDown(Keys.Right) ||
              kb.IsKeyDown(Keys.D),
      fireA:  kb.IsKeyDown(Keys.Z) ||
              kb.IsKeyDown(Keys.OemComma),
      fireB:  kb.IsKeyDown(Keys.X) ||
              kb.IsKeyDown(Keys.OemPeriod),
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
