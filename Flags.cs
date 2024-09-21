namespace Sonic 
{
  public class Flags
  {
    private BitArray _flags = new BitArray(8);

    public Flags();
    public Flags(byte value) => Byte = value;

    public byte Byte
    {
      get
      {
        var bytes = new byte[1];
        _flags.CopyTo(bytes, 0);
        return bytes;
      }
      set => _flags = new BitArray(new[] { value });
    }

    public bool Get(int index) => _flags[index];

    public Flags Set(int index, bool value)
    {
      _flags[index] = value;
      return _flags;
    } 

    public bool this[int index]
    {
      get => _flags[index];
      set => _flags[index] = value;
    }
  }
}