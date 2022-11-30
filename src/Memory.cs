using Quill.Extensions;

namespace Quill 
{
  public class Memory
  {
    private const ushort MaxAddress = ushort.MaxValue - Unusable; 
    private const ushort Unusable = 0x2000;
    private byte[] _memory;

    public Memory() => _memory = new byte[MaxAddress + 1];

    private byte Read(ushort address) => (byte)_memory[At(address)];
    private void Write(ushort address, byte value) => _memory[At(address)] = value;
    private void WriteWord(ushort address, ushort word)
    {
      _memory[address] = word.LowByte();
      _memory[address.Increment()] = word.HighByte();
    }
    private ushort At(ushort address) => (address > MaxAddress) ? (ushort)(address - Unusable) : address;
    
    public byte this[ushort address]
    {
      get => Read(address);
      set => Write(address, value);
    }
  }
}