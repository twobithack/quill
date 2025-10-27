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
  private bool _frontBufferToggle;
  private readonly byte[] _frontBufferA;
  private readonly byte[] _frontBufferB;
  private readonly int[] _backBuffer;
  private readonly bool[] _occupied;
  #endregion

  public Framebuffer()
  {
    _frontBufferA = new byte[FRONT_BUFFER_SIZE];
    _frontBufferB = new byte[FRONT_BUFFER_SIZE];
    _backBuffer = new int[BACK_BUFFER_SIZE];
    _occupied = new bool[BACK_BUFFER_SIZE];
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
    var bufferToggle = Volatile.Read(ref _frontBufferToggle);
    var frontBuffer = bufferToggle
                    ? _frontBufferA
                    : _frontBufferB;

    Buffer.BlockCopy(_backBuffer, 0, frontBuffer, 0, FRONT_BUFFER_SIZE);
    Volatile.Write(ref _frontBufferToggle, !bufferToggle);

    Array.Clear(_backBuffer);
    Array.Clear(_occupied);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public byte[] PopFrame() => !Volatile.Read(ref _frontBufferToggle) 
                            ? _frontBufferA 
                            : _frontBufferB;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static int GetPixelIndex(int x, int y) => x + (y * FRAME_WIDTH);
  #endregion
}