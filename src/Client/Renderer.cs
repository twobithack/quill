using System;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Quill.Common;

namespace Quill.Client;

public sealed class Renderer
{
  #region Constants
  private const int FRAMEBUFFER_WIDTH = 256;
  private const int FRAMEBUFFER_HEIGHT = 240;
  private const int BOTTOM_BORDER_HEIGHT = 48;
  private const int LEFT_BORDER_WIDTH = 8;
  private const float ASPECT_RATIO_CORRECTION = 8f / 7f;
  #endregion

  #region Fields
  private readonly Func<byte[]> _requestNextFrame;
  private readonly DisplayOptions _config;

  private readonly byte[] _framebuffer;
  private int _texture;
  private int _vao;
  private int _vbo;
  private int _shaderProgram;

  private int _uTextureSizeLoc;
  private int _uUVMinLoc;
  private int _uUVMaxLoc;
  private int _uEnableCRTLoc;
  #endregion

  public Renderer(Func<byte[]> requestNextFrame, DisplayOptions config)
  {
    _framebuffer = new byte[FRAMEBUFFER_WIDTH * FRAMEBUFFER_HEIGHT * 4];
    _requestNextFrame = requestNextFrame;
    _config = config;
  }

  #region Properties
  private int TextureWidth  => FRAMEBUFFER_WIDTH  - (_config.CropLeftBorder   ? LEFT_BORDER_WIDTH    : 0);
  private int TextureHeight => FRAMEBUFFER_HEIGHT - (_config.CropBottomBorder ? BOTTOM_BORDER_HEIGHT : 0);
  #endregion

  #region Methods
  public void Initialize()
  {
    GL.ClearColor(0f, 0f, 0f, 1f);

    _texture = GL.GenTexture();
    GL.BindTexture(TextureTarget.Texture2D, _texture);
    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,     (int)TextureWrapMode.ClampToEdge);
    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,     (int)TextureWrapMode.ClampToEdge);
    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                  FRAMEBUFFER_WIDTH, FRAMEBUFFER_HEIGHT, 0,
                  PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

    var uMin = _config.CropLeftBorder
             ? (float)LEFT_BORDER_WIDTH / FRAMEBUFFER_WIDTH
             : 0f;

    var vMax = _config.CropBottomBorder
             ? (float)(FRAMEBUFFER_HEIGHT - BOTTOM_BORDER_HEIGHT) / FRAMEBUFFER_HEIGHT
             : 1f;

    float[] vertices = [
      -1f,   1f, uMin,   0f,
      -1f,  -1f, uMin, vMax,
       1f,   1f,   1f,   0f,
       1f,  -1f,   1f, vMax
    ];

    _shaderProgram = CreateProgram();
    _vao = GL.GenVertexArray();
    _vbo = GL.GenBuffer();
    GL.BindVertexArray(_vao);
    GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
    GL.BufferData(BufferTarget.ArrayBuffer,
                  vertices.Length * sizeof(float),
                  vertices,
                  BufferUsageHint.StaticDraw);

    const int stride = 4 * sizeof(float);
    GL.EnableVertexAttribArray(0);
    GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
    GL.EnableVertexAttribArray(1);
    GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 2 * sizeof(float));

    GL.UseProgram(_shaderProgram);
    GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "uTexture"), 0);

    _uTextureSizeLoc = GL.GetUniformLocation(_shaderProgram, "uTextureSize");
    _uUVMinLoc       = GL.GetUniformLocation(_shaderProgram, "uUVMin");
    _uUVMaxLoc       = GL.GetUniformLocation(_shaderProgram, "uUVMax");
    _uEnableCRTLoc   = GL.GetUniformLocation(_shaderProgram, "uEnableCRT");
      
    GL.Uniform2(_uTextureSizeLoc, (float)TextureWidth, (float)TextureHeight);
    GL.Uniform2(_uUVMinLoc, uMin, 0f);
    GL.Uniform2(_uUVMaxLoc, 1f, vMax);
    GL.Uniform1(_uEnableCRTLoc, _config.EnableCRTFilter ? 1 : 0);

    GL.UseProgram(0);
  }

  public void UpdateFrame()
  {
    var frame = _requestNextFrame();
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
    var viewportAR = (float)dimensions.X / dimensions.Y;
    var textureAR = (float)TextureWidth / TextureHeight;

    if (_config.FixAspectRatio)
      textureAR *= ASPECT_RATIO_CORRECTION;

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

    void main()
    {
      vTexCoord = aTexCoord;
      gl_Position = vec4(aPosition, 0.0, 1.0);
    }";

  private const string FragmentShaderSource = @"#version 330 core
    in vec2   vTexCoord;
    out vec4  FragColor;

    uniform sampler2D uTexture;
    uniform vec2      uTextureSize;
    uniform vec2      uUVMin;
    uniform vec2      uUVMax;
    uniform int       uEnableCRT;

    const float GAMMA = 2.2;
    const float PI    = 3.14159265;

    vec3 toLinear(vec3 color) { return pow(color, vec3(GAMMA)); }
    vec3 toSRGB  (vec3 color) { return pow(color, vec3(1.0 / GAMMA)); }

    vec2 normalizeUV(vec2 uv, vec2 uvMin, vec2 uvMax)
    {
      vec2 visibleRange = max(uvMax - uvMin, vec2(1e-6));
      vec2 normalized   = (uv - uvMin) / visibleRange;
      return clamp(normalized, 0.0, 1.0);
    }

    vec3 applyHorizontalBlur(vec2 uv, vec2 texelSize, float intensity)
    { 
      vec2 texelWidth = vec2(texelSize.x, 0.0);
      vec3 center = texture(uTexture, uv).rgb;
      vec3 left   = texture(uTexture, uv - texelWidth).rgb;
      vec3 right  = texture(uTexture, uv + texelWidth).rgb;

      vec3 blurred = (center * 2 + left + right) * 0.25;
      return mix(center, blurred, intensity);
    }

    vec3 applyChromaticAberration(vec2 uv, vec2 texelSize, float intensity)
    {
      float offset = texelSize.x * intensity;
      vec2 redUV   = uv + vec2(-offset, 0.0);
      vec2 greenUV = uv;
      vec2 blueUV  = uv + vec2( offset, 0.0);

      return vec3(texture(uTexture, redUV).r,
                  texture(uTexture, greenUV).g,
                  texture(uTexture, blueUV).b);
    }

    float getScanlineIntensity(float normalizedY, float textureHeight)
    {
      float rowIndex      = normalizedY * textureHeight;
      float rowFraction   = fract(rowIndex);
      float cosineWave    = cos(PI * rowFraction);
      float cosineWaveSq  = pow(cosineWave, 2.0);

      return 0.55 + 0.45 * cosineWaveSq;
    }

    vec3 getShadowMask(float fragmentX)
    {
      float triad = mod(fragmentX, 3.0);
      if (triad < 1.0)      return vec3(1.00, 0.45, 0.45);
      else if (triad < 2.0) return vec3(0.45, 1.00, 0.45);
      else                  return vec3(0.45, 0.45, 1.00);
    }

    float getVignetteFactor(vec2 uv)
    {
      vec2  offset    = (uv - 0.5) * 1.2;
      float factor    = 1.0 - dot(offset, offset);
      return clamp(factor, 0.0, 1.0);
    }

    void main()
    {
      vec4 source = texture(uTexture, vTexCoord);
      if (uEnableCRT == 0)
      {
        FragColor = source;
        return;
      }

      vec2 normalizedUV = normalizeUV(vTexCoord, uUVMin, uUVMax);

      vec2 texelSize = 1.0 / uTextureSize;
      vec3 blurredColor = applyHorizontalBlur(vTexCoord, texelSize, 0.4);
      vec3 aberratedColor = applyChromaticAberration(vTexCoord, texelSize, 0.7);
      vec3 baseColor = mix(blurredColor, aberratedColor, 0.65);

      vec3 color = toLinear(baseColor);
      color *= getScanlineIntensity(normalizedUV.y, uTextureSize.y);
      color *= getShadowMask(gl_FragCoord.x);
      color *= getVignetteFactor(normalizedUV);

      vec3 srgb = toSRGB(color);
      FragColor = vec4(srgb, source.a);
    }";
  #endregion
}