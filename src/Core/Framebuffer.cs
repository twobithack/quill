using System;
using System.Runtime.CompilerServices;

using Quill.Common.Interfaces;

namespace Quill.Core;

unsafe public sealed class Framebuffer : IVideoSink
{
  #region Constants
  private const int FRAME_WIDTH = 256;
  private const int FRAME_HEIGHT = 240;
  private const int BYTES_PER_SCANLINE = FRAME_WIDTH * sizeof(int);
  private const int BUFFER_SIZE = BYTES_PER_SCANLINE * FRAME_HEIGHT;
  #endregion

  #region Fields
  private readonly byte[] _backBuffer;
  private readonly byte[] _frontBufferA;
  private readonly byte[] _frontBufferB;
  private volatile bool _frontBufferToggle;
  #endregion

  public Framebuffer()
  {
    _backBuffer = new byte[BUFFER_SIZE];
    _frontBufferA = new byte[BUFFER_SIZE];
    _frontBufferB = new byte[BUFFER_SIZE];
  }

  #region Methods
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void BlitScanline(int y, int[] scanline)
  {
    var offset = y * BYTES_PER_SCANLINE;
    Buffer.BlockCopy(scanline, 0, _backBuffer, offset, BYTES_PER_SCANLINE);
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void PresentFrame()
  {
    var targetBuffer = _frontBufferToggle
                     ? _frontBufferA
                     : _frontBufferB;

    Buffer.BlockCopy(_backBuffer, 0, targetBuffer, 0, BUFFER_SIZE);
    _frontBufferToggle = !_frontBufferToggle;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public byte[] ReadFrame() => _frontBufferToggle
                             ? _frontBufferB
                             : _frontBufferA;
  #endregion
}