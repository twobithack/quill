namespace Sonic 
{
  public class FlagsByte
  {
    private BitArray _flags = new BitArray(8);

    public FlagsByte();
    public FlagsByte(byte value) => Byte = value;

    public byte Byte
    {
      get
      {
        var bytes = new byte[1];
        _flags.CopyTo(bytes, 0);
        return bytes;
      }
      set
      {
        _flags = new BitArray(new[] { value })
      }
    }
    
    public bool this[int index]
    {
      get { return _flags.Get(index); }
      set { _flags.Set(index, value); }
    }
  }
}