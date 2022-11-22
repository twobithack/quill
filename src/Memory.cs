namespace Sonic 
{
  public class Memory
  {
    private const ushort MaxAddress = ushort.MaxValue - Unusable; 
    private const ushort Unusable = 0x2000;
    private byte[] _memory = new byte[MaxAddress];

    public Memory()
    {      
    }

    private byte Read(ushort address) => (byte)_memory[Resolve(address)];

    private void Write(ushort address, byte value) => _memory[Resolve(address)] = value;

    private ushort Resolve(ushort address) => (address > MaxAddress) ? (ushort)(address - Unusable) : address;
    
    public byte this[ushort index]
    {
      get => Read(index);
      set => Write(index, value);
    }
  }
}