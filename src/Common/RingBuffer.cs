namespace Quill.Common;

public sealed class RingBuffer<T>
{
  private readonly T[] _buffer;
  private int _bufferPosition;
  private int _bufferEnd;

  public RingBuffer(int capacity)
  {
    _buffer = new T[capacity];
  }

  public T Pop()
  {
    if (_bufferPosition != _bufferEnd)
    {
      _bufferPosition--;
      if (_bufferPosition < 0)
        _bufferPosition = _buffer.Length - 1;
    }

    return _buffer[_bufferPosition];
  }

  public void Push(T item)
  {
    _buffer[_bufferPosition] = item;
    _bufferPosition = (_bufferPosition + 1) % _buffer.Length;
    if (_bufferEnd == _bufferPosition)
      _bufferEnd = (_bufferEnd + 1) % _buffer.Length;
  }
}
