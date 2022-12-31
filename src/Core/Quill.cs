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
    var input = Keyboard.GetState();
    if (input.IsKeyDown(Keys.Escape))
      Exit();
    
    _emulator.Input.Joy1Up    = input.IsKeyDown(Keys.Up);
    _emulator.Input.Joy1Down  = input.IsKeyDown(Keys.Down);
    _emulator.Input.Joy1Left  = input.IsKeyDown(Keys.Left);
    _emulator.Input.Joy1Right = input.IsKeyDown(Keys.Right);
    _emulator.Input.Joy1FireA = input.IsKeyDown(Keys.Z);
    _emulator.Input.Joy1FireB = input.IsKeyDown(Keys.X);

    // TODO: Joypad 2

    _framebuffer
      .SetData<byte>(_emulator.ReadFramebuffer());
    
    base.Update(gameTime);
  }

  protected override void Draw(GameTime gameTime)
  {
    GraphicsDevice.Clear(Color.Black);
    _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
    _spriteBatch.Draw(_framebuffer, GraphicsDevice.Viewport.Bounds, Color.White);
    _spriteBatch.End();
    base.Draw(gameTime);
  }
}
