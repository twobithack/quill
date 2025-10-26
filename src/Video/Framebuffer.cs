using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Quill.Video;

unsafe public sealed class Framebuffer
{
  #region Constants
  private const int FRAME_WIDTH = 256;
  private const int FRAME_HEIGHT = 240;
  private const int BACK_BUFFER_SIZE = FRAME_WIDTH * FRAME_HEIGHT;
  private const int FRONT_BUFFER_SIZE = BACK_BUFFER_SIZE * sizeof(int);
  #endregion

  #region Fields
  private readonly byte[] _frontBuffer;
  private readonly int[] _backBuffer;
  private readonly bool[] _occupied;
  private readonly Lock _bufferLock;
  #endregion

  public Framebuffer()
  {
    _frontBuffer = new byte[FRONT_BUFFER_SIZE];
    _backBuffer = new int[BACK_BUFFER_SIZE];
    _occupied = new bool[BACK_BUFFER_SIZE];
    _bufferLock = new Lock();
  }

  #region Methods
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void SetPixel(int x, int y, int value, bool isSprite)
  {
    var index = GetPixelIndex(x, y);
    _backBuffer[index] = value;
    _occupied[index] = isSprite;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool IsOccupied(int x, int y) => _occupied[GetPixelIndex(x, y)];

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void PushFrame()
  {
    lock (_bufferLock)
      Buffer.BlockCopy(_backBuffer, 0, _frontBuffer, 0, FRONT_BUFFER_SIZE);

    Array.Clear(_backBuffer);
    Array.Clear(_occupied);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public byte[] PopFrame()
  {
    lock (_bufferLock)
      return _frontBuffer;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static int GetPixelIndex(int x, int y) => x + (y * FRAME_WIDTH);
  #endregion
}