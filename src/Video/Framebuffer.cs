using System;
using System.Runtime.CompilerServices;

namespace Quill.Video;

unsafe public sealed class Framebuffer
{
  #region Constants
  private const int FRAME_WIDTH = 256;
  private const int FRAME_HEIGHT = 240;
  private const int FRAMEBUFFER_SIZE = FRAME_WIDTH *
                                       FRAME_HEIGHT *
                                       sizeof(int);
  #endregion

  #region Fields
  private readonly int[] _pixelbuffer;
  private readonly bool[] _occupied;
  private readonly byte[] _framebuffer;
  private bool _frameQueued;
  #endregion

  public Framebuffer()
  {
    _framebuffer = new byte[FRAMEBUFFER_SIZE];
    _pixelbuffer = new int[FRAME_WIDTH * FRAME_HEIGHT];
    _occupied = new bool[FRAME_WIDTH * FRAME_HEIGHT];
  }

  #region Methods
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void SetPixel(int x, int y, int value, bool isSprite)
  {
    var index = GetPixelIndex(x, y);
    _pixelbuffer[index] = value;
    _occupied[index] = isSprite;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool IsOccupied(int x, int y) => _occupied[GetPixelIndex(x, y)];

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void PushFrame()
  {
    lock (_framebuffer)
    {
      Buffer.BlockCopy(_pixelbuffer, 0, _framebuffer, 0, FRAMEBUFFER_SIZE);
      _frameQueued = true;
    }

    Array.Clear(_pixelbuffer);
    Array.Clear(_occupied);
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
  private static int GetPixelIndex(int x, int y) => x + (y * FRAME_WIDTH);
  #endregion
}