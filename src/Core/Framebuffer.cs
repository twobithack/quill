using System;
using System.Runtime.CompilerServices;
using System.Threading;

using Quill.Common.Interfaces;

namespace Quill.Core;

unsafe public sealed class Framebuffer : IVideoSink
{
  #region Constants
  private const int FRAME_WIDTH = 256;
  private const int FRAME_HEIGHT = 240;
  private const int BACK_BUFFER_SIZE = FRAME_WIDTH * FRAME_HEIGHT;
  private const int FRONT_BUFFER_SIZE = BACK_BUFFER_SIZE * sizeof(int);
  #endregion

  #region Fields
  private readonly int[] _backBuffer;
  private readonly byte[] _frontBufferA;
  private readonly byte[] _frontBufferB;
  private bool _frontBufferToggle;
  #endregion

  public Framebuffer()
  {
    _backBuffer = new int[BACK_BUFFER_SIZE];
    _frontBufferA = new byte[FRONT_BUFFER_SIZE];
    _frontBufferB = new byte[FRONT_BUFFER_SIZE];
  }

  #region Methods
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void SubmitPixel(int x, int y, int value)
  {
    var index = GetPixelIndex(x, y);
    _backBuffer[index] = value;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void PublishFrame()
  {
    var bufferToggle = Volatile.Read(ref _frontBufferToggle);
    var targetBuffer = bufferToggle
                     ? _frontBufferA
                     : _frontBufferB;

    Buffer.BlockCopy(_backBuffer, 0, targetBuffer, 0, FRONT_BUFFER_SIZE);
    Volatile.Write(ref _frontBufferToggle, !bufferToggle);
    Array.Clear(_backBuffer);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public byte[] ReadFrame() => Volatile.Read(ref _frontBufferToggle) 
                            ? _frontBufferB
                            : _frontBufferA;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static int GetPixelIndex(int x, int y) => x + (y * FRAME_WIDTH);
  #endregion
}