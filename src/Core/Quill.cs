using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Quill;

public class Quill : Game
{
  private GraphicsDeviceManager _graphics;
  private SpriteBatch _spriteBatch;
  private Texture2D _display;
  private Emulator _emulator;
  private Thread _emulationThread;

  public Quill(string filepath)
  {
    _emulator = new Emulator(filepath);
    _graphics = new GraphicsDeviceManager(this);
    Content.RootDirectory = "content";
  }

  protected override void Initialize()
  {
    _display = new Texture2D(GraphicsDevice, 256, 192);
    _emulationThread = new Thread(_emulator.Run);
    _emulationThread.Start();

    _graphics.PreferredBackBufferWidth = 256;
    _graphics.PreferredBackBufferHeight = 192;
    _graphics.ApplyChanges();

    base.Initialize();
  }

  protected override void LoadContent() => _spriteBatch = new SpriteBatch(GraphicsDevice);

  protected override void UnloadContent() => _emulationThread.Abort();

  protected override void Update(GameTime gameTime)
  {
    if (Keyboard.GetState().IsKeyDown(Keys.Escape))
      Exit();

    var buffer = _emulator.GetFramebuffer();
    if (buffer != null)
      _display.SetData<byte>(buffer);

    base.Update(gameTime);
  }

  protected override void Draw(GameTime gameTime)
  {
    GraphicsDevice.Clear(Color.Black);
    _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
    _spriteBatch.Draw(_display, GraphicsDevice.Viewport.Bounds, Color.White);
    _spriteBatch.End();
    base.Draw(gameTime);
  }
}
