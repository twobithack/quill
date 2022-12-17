using System.Runtime.CompilerServices;
using Quill.Extensions;

namespace Quill.Z80
{
  public class Memory
  {
    private const ushort MaxAddress = ushort.MaxValue - Unusable; 
    private const ushort Unusable = 0x2000;
    private byte[] _memory;

    public Memory() => _memory = new byte[MaxAddress + 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte(ushort address) => (byte)_memory[Map(address)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadWord(ushort address)
    {
      var lowByte = ReadByte(address);
      var highByte = ReadByte(address.Increment());
      return highByte.Concat(lowByte);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(ushort address, byte value) => _memory[Map(address)] = value;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteWord(ushort address, ushort word)
    {
      WriteByte(address, word.GetLowByte());
      WriteByte(address.Increment(), word.GetHighByte());
    }

    public void Dump(string path)
    {
      var dump = new List<string>();
      for (byte row = 0; row < byte.MaxValue; row++)
        dump.Add(DumpPage(row));
        
      File.AppendAllLines(path, dump);
    }

    public string DumpPage(byte page)
    {
      var row = string.Empty;
      for (byte col = 0; col < byte.MaxValue; col++)
        row += ReadByte(page.Concat(col)).ToHex() + " ";
      return row;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort Map(ushort address) => (address > MaxAddress) ? (ushort)(address - Unusable) : address;
  }
}