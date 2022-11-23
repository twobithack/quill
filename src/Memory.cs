namespace Sonic 
{
  public class Memory
  {
    private const ushort MaxAddress = ushort.MaxValue - Unusable; 
    private const ushort Unusable = 0x2000;
    private byte[] _memory;

    public Memory()
    {
      _memory = new byte[MaxAddress + 1];
    }

    private byte Read(ushort address) => (byte)_memory[Fix(address)];

    private void Write(ushort address, byte value) => _memory[Fix(address)] = value;

    private ushort Fix(ushort address) => (address > MaxAddress) ? (ushort)(address - Unusable) : address;
    
    public byte this[ushort address]
    {
      get => Read(address);
      set => Write(address, value);
    }
  }
}