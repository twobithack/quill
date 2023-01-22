namespace Quill.Core;

public sealed class RewindBuffer
{
  private readonly Snapshot[] _buffer;
  private int _bufferPosition;
  private int _bufferEnd;

  public RewindBuffer(int capacity)
  {
    _buffer = new Snapshot[capacity];
  }

  public Snapshot Pop()
  {
    if (_bufferPosition != _bufferEnd)
    {
      _bufferPosition--;
      if (_bufferPosition < 0)
        _bufferPosition = _buffer.Length - 1;
    }

    return _buffer[_bufferPosition];
  }

  public void Push(Snapshot state)
  {
    _buffer[_bufferPosition] = state;
    _bufferPosition = (_bufferPosition + 1) % _buffer.Length;
    if (_bufferEnd == _bufferPosition)
      _bufferEnd = (_bufferEnd + 1) % _buffer.Length;
  }
}
