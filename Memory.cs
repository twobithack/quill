namespace Sonic 
{
  public class Memory
  {
    private const int MaxAddress = 0xDFFF;
    private const int MirrorSize = 0x2000;

    private byte[] _memory;

    public Memory()
    {
      _memory = new byte[MaxAddress];
    }

    private byte Read(int address)
    {
      if (address > MaxAddress)
      {
        return _memory[address - MirrorSize];
      }

      return _memory[address];
    }

    private void Write(int address, byte value)
    {
      if (address > MaxAddress)
      {
        return _memory[address - MirrorSize];
      }

      return _memory[address];
    }
    
    public byte this[int index]
    {
      get => Read(index);
      set => Write(index, value);
    }
  }
}