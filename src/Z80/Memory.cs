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
    public byte ReadByte(ushort address) => (byte)_memory[Resolve(address)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadWord(ushort address)
    {
      var lowByte = ReadByte(address);
      var highByte = ReadByte(address.Increment());
      return highByte.Concat(lowByte);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(ushort address, byte value) => _memory[Resolve(address)] = value;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteWord(ushort address, ushort word)
    {
      WriteByte(address, word.GetLowByte());
      WriteByte(address.Increment(), word.GetHighByte());
    }

    public string DumpPage(byte page)
    {
      var row = string.Empty;
      for (byte index = 0; index < byte.MaxValue; index++)
        row += ReadByte(page.Concat(index)).ToHex() + " ";
      return row;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort Resolve(ushort address) => (address > MaxAddress) ? (ushort)(address - Unusable) : address;
  }
}