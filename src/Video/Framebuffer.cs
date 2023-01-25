using System;
using System.Runtime.CompilerServices;

namespace Quill.Video;

unsafe public sealed class Framebuffer
{
  private const int FRAMEBUFFER_SIZE = 0x30000;
  private readonly bool[] _containsSprite;
  private readonly byte[] _frame;
  private readonly int[] _buffer;
  private readonly int _width;
  private bool _frameQueued;

  public Framebuffer(int width, int height)
  {
    _buffer = new int[width * height];
    _containsSprite = new bool[width * height];
    _frame = new byte[FRAMEBUFFER_SIZE];
    _width = width;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void SetPixel(int x, int y, int rgba, bool isSprite)
  {
    var pixelIndex = GetIndex(x, y);
    _buffer[pixelIndex] = rgba;
    _containsSprite[pixelIndex] = isSprite;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool CheckCollision(int x, int y) => _containsSprite[GetIndex(x, y)];

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void PushFrame()
  {
    lock (_frame)
    {
      Buffer.BlockCopy(_buffer, 0, _frame, 0, FRAMEBUFFER_SIZE);
      _frameQueued = true;
    }

    Array.Clear(_buffer);
    Array.Clear(_containsSprite);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public byte[] PopFrame()
  {      
    if (!_frameQueued)
      return null;

    lock (_frame)
    {
      _frameQueued = false;
      return _frame;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private int GetIndex(int x, int y) => (x + (y * _width));
}