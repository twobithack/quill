namespace Quill.Common;

public sealed class RingBuffer<T> where T : new()
{
  private readonly T[] _buffer;
  private int _head;
  private int _tail;

  public RingBuffer(int capacity, bool preallocate)
  {
    _buffer = new T[capacity];

    if (preallocate)
      for (var i = 0; i < capacity; i++)
        _buffer[i] = new T();
  }

  public T Pop()
  {
    if (_head != _tail)
    {
      _head--;
      if (_head < 0)
        _head = _buffer.Length - 1;
    }

    return _buffer[_head];
  }

  public void Push(T item)
  {
    _buffer[_head] = item;
    _head = (_head + 1) % _buffer.Length;
    if (_tail == _head)
      _tail = (_tail + 1) % _buffer.Length;
  }

  public ref T AcquireSlot()
  {
    ref T slot = ref _buffer[_head];
    _head = (_head + 1) % _buffer.Length;
    if (_tail == _head)
      _tail = (_tail + 1) % _buffer.Length;
    return ref slot;
  }
}
