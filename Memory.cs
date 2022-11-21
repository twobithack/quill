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

    private byte Get(int address)
    {
      if (address > MaxAddress)
      {
        return _memory[address - MirrorSize];
      }

      return _memory[address];
    }

    private void Set(int address, byte value)
    {
      if (address > MaxAddress)
      {
        return _memory[address - MirrorSize];
      }

      return _memory[address];
    }
    
    public byte this[int index]
    {
      get => return Get(index);
      set => Set(index, value);
    }
  }
}