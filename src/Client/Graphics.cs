using System;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Quill.Common;

namespace Quill.Client;

public class Graphics
{
  #region Constants
  private const int FRAMEBUFFER_WIDTH = 256;
  private const int FRAMEBUFFER_HEIGHT = 240;
  private const int BOTTOM_BORDER_HEIGHT = 48;
  private const int LEFT_BORDER_WIDTH = 8;
  #endregion

  #region Fields
  private readonly Func<byte[]> _requestNextFrame;
  private readonly Configuration _configuration;
  
  private readonly byte[] _framebuffer;
  private int _texture;
  private int _vao;
  private int _vbo;
  private int _shaderProgram;
  #endregion

  public Graphics(Func<byte[]> requestNextFrame, Configuration config)
  {
    _requestNextFrame = requestNextFrame;
    _configuration = config;
    
    _framebuffer = new byte[FRAMEBUFFER_WIDTH * FRAMEBUFFER_HEIGHT * 4];
  }

  #region Properties
  private int TextureWidth => FRAMEBUFFER_WIDTH - (_configuration.CropLeftBorder ? LEFT_BORDER_WIDTH : 0);
  private int TextureHeight => FRAMEBUFFER_HEIGHT - (_configuration.CropBottomBorder ? BOTTOM_BORDER_HEIGHT : 0);
  #endregion

  #region Methods
  public void Initialize()
  {
    GL.ClearColor(0f, 0f, 0f, 1f);

    _texture = GL.GenTexture();
    GL.BindTexture(TextureTarget.Texture2D, _texture);
    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                  FRAMEBUFFER_WIDTH, FRAMEBUFFER_HEIGHT, 0,
                  PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

    var uMin = _configuration.CropLeftBorder
               ? (float) LEFT_BORDER_WIDTH / FRAMEBUFFER_WIDTH
               : 0f;

    var vMax = _configuration.CropBottomBorder
               ? (float) (FRAMEBUFFER_HEIGHT - BOTTOM_BORDER_HEIGHT) / FRAMEBUFFER_HEIGHT
               : 1f;

    float[] vertices = {
      -1f,   1f, uMin,   0f,
      -1f,  -1f, uMin, vMax,
       1f,   1f,   1f,   0f,
       1f,  -1f,   1f, vMax
    };

    _shaderProgram = CreateProgram();
    _vao = GL.GenVertexArray();
    _vbo = GL.GenBuffer();
    GL.BindVertexArray(_vao);
    GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
    GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float),
                  vertices, BufferUsageHint.StaticDraw);

    var posLoc = GL.GetAttribLocation(_shaderProgram, "aPosition");
    GL.EnableVertexAttribArray(posLoc);
    GL.VertexAttribPointer(posLoc, 2, VertexAttribPointerType.Float,
                           false, 4 * sizeof(float), 0);

    var texLoc = GL.GetAttribLocation(_shaderProgram, "aTexCoord");
    GL.EnableVertexAttribArray(texLoc);
    GL.VertexAttribPointer(texLoc, 2, VertexAttribPointerType.Float,
                           false, 4 * sizeof(float), 2 * sizeof(float));

    GL.UseProgram(_shaderProgram);
    var uni = GL.GetUniformLocation(_shaderProgram, "uTexture");
    GL.Uniform1(uni, 0);
    GL.UseProgram(0);
  }

  public void UpdateFrame()
  {
    var frame = _requestNextFrame();
    if (frame == null)
      return;

    System.Buffer.BlockCopy(frame, 0, _framebuffer, 0, _framebuffer.Length);
    GL.BindTexture(TextureTarget.Texture2D, _texture);
    GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0,
                     FRAMEBUFFER_WIDTH, FRAMEBUFFER_HEIGHT,
                     PixelFormat.Rgba, PixelType.UnsignedByte, _framebuffer);
  }

  public void RenderFrame()
  {
    GL.Clear(ClearBufferMask.ColorBufferBit);
    GL.UseProgram(_shaderProgram);
    GL.ActiveTexture(TextureUnit.Texture0);
    GL.BindTexture(TextureTarget.Texture2D, _texture);
    GL.BindVertexArray(_vao);
    GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
  }

  public void ResizeViewport(Vector2i dimensions)
  {
    var textureAR = (float) TextureWidth / TextureHeight;
    var viewportAR = (float) dimensions.X / dimensions.Y;

    int width, height, x, y;
    if (viewportAR > textureAR)
    {
      height = dimensions.Y;
      width = (int)(height * textureAR);
      x = (dimensions.X - width) / 2;
      y = 0;
    }
    else
    {
      width = dimensions.X;
      height = (int)(width / textureAR);
      y = (dimensions.Y - height) / 2;
      x = 0;
    }
    
    GL.Viewport(x, y, width, height);
  }
 
  public void Stop()
  {
    GL.DeleteBuffer(_vbo);
    GL.DeleteVertexArray(_vao);
    GL.DeleteTexture(_texture);
    GL.DeleteProgram(_shaderProgram);
  }

  private static int CreateProgram()
  {
    var vs = GL.CreateShader(ShaderType.VertexShader);
    GL.ShaderSource(vs, VertexShaderSource);
    GL.CompileShader(vs);

    var fs = GL.CreateShader(ShaderType.FragmentShader);
    GL.ShaderSource(fs, FragmentShaderSource);
    GL.CompileShader(fs);

    var program = GL.CreateProgram();
    GL.AttachShader(program, vs);
    GL.AttachShader(program, fs);
    GL.LinkProgram(program);
    GL.DeleteShader(vs);
    GL.DeleteShader(fs);
    return program;
  }
  #endregion

  #region Shaders
  private const string VertexShaderSource = @"#version 330 core
                                            layout(location = 0) in vec2 aPosition;
                                            layout(location = 1) in vec2 aTexCoord;
                                            out vec2 vTexCoord;
                                            void main() {
                                                vTexCoord = aTexCoord;
                                                gl_Position = vec4(aPosition, 0.0, 1.0);
                                            }";

  private const string FragmentShaderSource = @"#version 330 core
                                              in vec2 vTexCoord;
                                              out vec4 FragColor;
                                              uniform sampler2D uTexture;
                                              void main() {
                                                  FragColor = texture(uTexture, vTexCoord);
                                              }";
  #endregion
}