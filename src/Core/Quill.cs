using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Quill;

public class Quill : Game
{
  private GraphicsDeviceManager _graphics;
  private SpriteBatch _spriteBatch;
  private Texture2D _framebuffer;
  private Emulator _emulator;
  private Thread _emulationThread;

  public Quill(string filepath)
  {
    _emulator = new Emulator(filepath);
    _graphics = new GraphicsDeviceManager(this);
    Content.RootDirectory = "content";
    IsMouseVisible = true;
  }

  protected override void Initialize()
  {
    _framebuffer = new Texture2D(GraphicsDevice, 256, 192);
    _emulationThread = new Thread(_emulator.Run);
    _emulationThread.Start();

    _graphics.PreferredBackBufferWidth = 1024;
    _graphics.PreferredBackBufferHeight = 768;
    _graphics.ApplyChanges();
    base.Initialize();
  }

  protected override void LoadContent() => _spriteBatch = new SpriteBatch(GraphicsDevice);

  #pragma warning disable SYSLIB0006
  protected override void UnloadContent() => _emulationThread.Abort();
  #pragma warning restore SYSLIB0006

  protected override void Update(GameTime gameTime)
  {
    var joy1 = GamePad.GetState(0);
    if (joy1.IsConnected)
    {
      _emulator.Input.Joy1Up = joy1.IsButtonDown(Buttons.DPadUp) ||
                               joy1.IsButtonDown(Buttons.LeftThumbstickUp);
      _emulator.Input.Joy1Left = joy1.IsButtonDown(Buttons.DPadLeft) ||
                                 joy1.IsButtonDown(Buttons.LeftThumbstickLeft);
      _emulator.Input.Joy1Down = joy1.IsButtonDown(Buttons.DPadDown) ||
                                 joy1.IsButtonDown(Buttons.LeftThumbstickDown);
      _emulator.Input.Joy1Right = joy1.IsButtonDown(Buttons.DPadRight) ||
                                  joy1.IsButtonDown(Buttons.LeftThumbstickRight);
      _emulator.Input.Joy1FireA = joy1.IsButtonDown(Buttons.A) || 
                                  joy1.IsButtonDown(Buttons.X);
      _emulator.Input.Joy1FireB = joy1.IsButtonDown(Buttons.B) || 
                                  joy1.IsButtonDown(Buttons.Y);
    }
    else
    {
      var kb = Keyboard.GetState();
      if (kb.IsKeyDown(Keys.Escape))
        Exit();

      _emulator.Input.Joy1Up = kb.IsKeyDown(Keys.Up) || 
                               kb.IsKeyDown(Keys.W);
      _emulator.Input.Joy1Left = kb.IsKeyDown(Keys.Left) || 
                                 kb.IsKeyDown(Keys.A);
      _emulator.Input.Joy1Down = kb.IsKeyDown(Keys.Down) || 
                                 kb.IsKeyDown(Keys.S);
      _emulator.Input.Joy1Right = kb.IsKeyDown(Keys.Right) || 
                                  kb.IsKeyDown(Keys.D);
      _emulator.Input.Joy1FireA = kb.IsKeyDown(Keys.Z) || 
                                  kb.IsKeyDown(Keys.OemComma);
      _emulator.Input.Joy1FireB = kb.IsKeyDown(Keys.X) || 
                                  kb.IsKeyDown(Keys.OemPeriod);
    }

    base.Update(gameTime);
  }

  protected override void Draw(GameTime gameTime)
  {
    GraphicsDevice.Clear(Color.Black);
    _framebuffer.SetData<byte>(_emulator.ReadFramebuffer());
    _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
    _spriteBatch.Draw(_framebuffer, GraphicsDevice.Viewport.Bounds, Color.White);
    _spriteBatch.End();
    base.Draw(gameTime);
  }
}
