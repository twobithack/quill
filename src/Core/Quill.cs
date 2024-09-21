using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Quill;

public class Quill : Game
{
  private const int BORDER_MASK_WIDTH = 8;
  private const int FRAMEBUFFER_WIDTH = 256;
  private const int FRAMEBUFFER_HEIGHT = 192;

  private readonly Emulator _emulator;
  private Thread _emulationThread;

  private GraphicsDeviceManager _graphics;
  private SpriteBatch _spriteBatch;
  private Texture2D _framebuffer;
  private Rectangle _viewport;
  private bool _maskBorder;
  private int _scale;
  
  public Quill(string romPath, bool maskBorder, int scale = 4)
  {
    _emulator = new Emulator(romPath);
    _graphics = new GraphicsDeviceManager(this);
    _maskBorder = maskBorder;
    _scale = scale;

    Content.RootDirectory = "content";
    IsMouseVisible = true;
  }

  protected override void Initialize()
  {
    _emulationThread = new Thread(_emulator.Run);
    _emulationThread.Start();

    _framebuffer = new Texture2D(GraphicsDevice, FRAMEBUFFER_WIDTH, FRAMEBUFFER_HEIGHT);
    _graphics.PreferredBackBufferHeight = _scale * FRAMEBUFFER_HEIGHT;
    _graphics.PreferredBackBufferWidth = _scale * FRAMEBUFFER_WIDTH;
    _graphics.ApplyChanges();    
    _viewport = GraphicsDevice.Viewport.Bounds;

    if (_maskBorder)
    {
      var offset = _scale * BORDER_MASK_WIDTH;
      _viewport.X -= offset;
      _graphics.PreferredBackBufferWidth -= offset;
      _graphics.ApplyChanges();    
    }

    base.Initialize();
  }

  protected override void LoadContent() => _spriteBatch = new SpriteBatch(GraphicsDevice);

  #pragma warning disable SYSLIB0006
  protected override void UnloadContent() => _emulationThread.Abort();
  #pragma warning restore SYSLIB0006

  protected override void Update(GameTime gameTime)
  {
    if (!ReadJoystickInput())
      ReadKeyboardInput();

    base.Update(gameTime);
  }

  protected override void Draw(GameTime gameTime)
  {
    var frame = _emulator.ReadFramebuffer();
    if (frame != null)
    {
      GraphicsDevice.Clear(Color.Black);
      _framebuffer.SetData(frame);

      _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
      _spriteBatch.Draw(_framebuffer, _viewport, Color.White);
      _spriteBatch.End();
    }

    base.Draw(gameTime);
  }

  private bool ReadJoystickInput()
  {
    var joy1 = GamePad.GetState(0);
    if (!joy1.IsConnected)
      return false;

    _emulator.Input.Joy1Up    = joy1.IsButtonDown(Buttons.DPadUp) ||
                                joy1.IsButtonDown(Buttons.LeftThumbstickUp);
    _emulator.Input.Joy1Left  = joy1.IsButtonDown(Buttons.DPadLeft) ||
                                joy1.IsButtonDown(Buttons.LeftThumbstickLeft);
    _emulator.Input.Joy1Down  = joy1.IsButtonDown(Buttons.DPadDown) ||
                                joy1.IsButtonDown(Buttons.LeftThumbstickDown);
    _emulator.Input.Joy1Right = joy1.IsButtonDown(Buttons.DPadRight) ||
                                joy1.IsButtonDown(Buttons.LeftThumbstickRight);
    _emulator.Input.Joy1FireA = joy1.IsButtonDown(Buttons.A) ||
                                joy1.IsButtonDown(Buttons.X);
    _emulator.Input.Joy1FireB = joy1.IsButtonDown(Buttons.B) ||
                                joy1.IsButtonDown(Buttons.Y);
    return true;
  }

  private void ReadKeyboardInput()
  {
    var kb = Keyboard.GetState();
    if (kb.IsKeyDown(Keys.Escape))
      Exit();
      
    _emulator.Input.Joy1Up    = kb.IsKeyDown(Keys.Up) ||
                                kb.IsKeyDown(Keys.W);
    _emulator.Input.Joy1Left  = kb.IsKeyDown(Keys.Left) ||
                                kb.IsKeyDown(Keys.A);
    _emulator.Input.Joy1Down  = kb.IsKeyDown(Keys.Down) ||
                                kb.IsKeyDown(Keys.S);
    _emulator.Input.Joy1Right = kb.IsKeyDown(Keys.Right) ||
                                kb.IsKeyDown(Keys.D);
    _emulator.Input.Joy1FireA = kb.IsKeyDown(Keys.Z) ||
                                kb.IsKeyDown(Keys.OemComma);
    _emulator.Input.Joy1FireB = kb.IsKeyDown(Keys.X) ||
                                kb.IsKeyDown(Keys.OemPeriod);
  }
}
