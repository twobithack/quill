namespace Quill.Common;

public sealed class RingBuffer<T> where T : new()
{
  private readonly T[] _buffer;
  private int _head;
  private int _tail;

  public RingBuffer(int capacity)
  {
    _buffer = new T[capacity];
    for (var i = 0; i < capacity; i++)
      _buffer[i] = new T();
  }

  public ref T AcquireSlot()
  {
    ref T slot = ref _buffer[_head];
    
    _head = Increment(_head);
    if (_head == _tail)
      _tail = Increment(_tail);

    return ref slot;
  }

  public T Pop()
  {
    if (_head != _tail)
      _head = Decrement(_head);

    return _buffer[_head];
  }

  private int Increment(int index) => (index + 1) % _buffer.Length;
  private int Decrement(int index) => (index + _buffer.Length - 1) % _buffer.Length;
}