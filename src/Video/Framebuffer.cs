using System;
using System.Runtime.CompilerServices;

namespace Quill.Video;

unsafe public sealed class Framebuffer
{
  private const int FRAMEBUFFER_SIZE = 0x30000;
  private readonly bool[] _containsSprite;
  private readonly byte[] _framebuffer;
  private readonly int[] _pixelbuffer;
  private readonly int _width;
  private bool _frameQueued;

  public Framebuffer(int width, int height)
  {
    _framebuffer = new byte[FRAMEBUFFER_SIZE];
    _pixelbuffer = new int[width * height];
    _containsSprite = new bool[width * height];
    _width = width;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void SetPixel(int x, int y, int rgba, bool isSprite)
  {
    var pixelIndex = GetPixelIndex(x, y);
    _pixelbuffer[pixelIndex] = rgba;
    _containsSprite[pixelIndex] = isSprite;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void SetLegacyPixel(int x, int y, byte colorIndex, bool isSprite)
  {
    var pixelIndex = GetPixelIndex(x, y);
    _pixelbuffer[pixelIndex] = Color.ToLegacyRGBA(colorIndex);
    _containsSprite[pixelIndex] = isSprite;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool CheckCollision(int x, int y) => _containsSprite[GetPixelIndex(x, y)];

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void PushFrame()
  {
    lock (_framebuffer)
    {
      Buffer.BlockCopy(_pixelbuffer, 0, _framebuffer, 0, FRAMEBUFFER_SIZE);
      _frameQueued = true;
    }

    Array.Clear(_pixelbuffer);
    Array.Clear(_containsSprite);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public byte[] PopFrame()
  {      
    if (!_frameQueued)
      return null;

    lock (_framebuffer)
    {
      _frameQueued = false;
      return _framebuffer;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private int GetPixelIndex(int x, int y) => (x + (y * _width));
}