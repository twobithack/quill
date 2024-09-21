using Quill.Extensions;

namespace Quill.Z80
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

    public void Dump(byte pages)
    {
      for (byte page = 0x00; page <= pages; page++)
        DumpPage(page);
    }

    public void DumpPage(byte page)
    {
      var row = "";
      for (byte col = 0x00; col < byte.MaxValue; col++)
        row += _memory[At(page.Append(col))].ToHex() + " ";
      Console.WriteLine(row);
    }
  }
}