using Quill.Extensions;

namespace Quill
{
  public class Memory
  {
    private const ushort MaxAddress = ushort.MaxValue - Unusable; 
    private const ushort Unusable = 0x2000;
    private byte[] _memory;

    public Memory() => _memory = new byte[MaxAddress + 1];

    public void WriteWord(ushort address, ushort word)
    {
      _memory[address] = word.GetLowByte();
      _memory[address.Increment()] = word.GetHighByte();
    }

    private ushort At(ushort address) => (address > MaxAddress) ? (ushort)(address - Unusable) : address;
    private byte Read(ushort address) => (byte)_memory[At(address)];
    private void Write(ushort address, byte value) => _memory[At(address)] = value;
    
    public byte this[ushort address]
    {
      get => Read(address);
      set => Write(address, value);
    }
  }
}