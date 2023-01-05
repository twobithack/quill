using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Quill;

public class Quill : Game
{
  #region Constants
  private const int FRAMEBUFFER_WIDTH = 256;
  private const int FRAMEBUFFER_HEIGHT = 192;
  private const int BORDER_MASK_WIDTH = 8;
  private const int PLAYER_1 = 0;
  private const int PLAYER_2 = 1;
  #endregion

  #region Fields
  private readonly Emulator _emulator;
  private readonly Thread _emulationThread;
  private readonly GraphicsDeviceManager _graphics;
  private SpriteBatch _spriteBatch;
  private Texture2D _framebuffer;
  private Rectangle _viewport;
  private readonly bool _cropBorder;
  private readonly int _scale;
  #endregion

  public Quill(byte[] rom,
               bool fixSlowdown = true,
               bool cropBorders = true, 
               int scaleFactor = 4)
  {
    Content.RootDirectory = "content";
    _emulator = new Emulator(rom, fixSlowdown);
    _emulationThread = new Thread(_emulator.Run);
    _graphics = new GraphicsDeviceManager(this);
    _cropBorder = cropBorders;
    _scale = scaleFactor;
  }

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

    _emulationThread.Start();
    base.Initialize();
  }

  protected override void LoadContent() => _spriteBatch = new SpriteBatch(GraphicsDevice);

  #pragma warning disable SYSLIB0006
  protected override void UnloadContent() => _emulationThread.Abort();
  #pragma warning restore SYSLIB0006

  protected override void Update(GameTime gameTime)
  {
    ReadInput();
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

  private void ReadInput()
  {
    if (ReadJoypadInput(PLAYER_1))
      ReadJoypadInput(PLAYER_2);
    else
      ReadKeyboardInput();
  }

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
              joypad.IsButtonDown(Buttons.X)
    );
    return true;
  }

  private void ReadKeyboardInput()
  {
    var kb = Keyboard.GetState();
    if (kb.IsKeyDown(Keys.Escape))
      Exit();

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
              kb.IsKeyDown(Keys.OemPeriod)
    );
  }
  #endregion
}
