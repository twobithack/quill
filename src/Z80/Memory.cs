using System.Runtime.CompilerServices;
using Quill.Extensions;

namespace Quill.Z80
{
  public class Memory
  {
    private byte[,] _memory;
    private byte[,] _rom;
    private byte _page0;
    private byte _page1;
    private byte _page2;
    private bool _ramEnable;
    private int _ramPage;

    public Memory()
    {
      _memory = new byte[0x03, 0x4000];
      _rom = new byte[0x40, 0x4000];
    }

    public void LoadROM(byte[] rom)
    {
      for (int i = 0; i < rom.Count(); i++)
        _rom[i / 0x4000, i % 0x4000] = rom[i];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte(ushort address)
    {
      if (address < 0x400)
        return _rom[0x00, address];

      if (address < 0x4000)
        return _rom[_page0, address];

      if (address < 0x8000)
        return _rom[_page1, address - 0x4000];
        
      if (address < 0xC000)
      {
        if (!_ramEnable)
          return _rom[_page2, address - 0x8000];

        return _memory[_ramPage, address - 0x8000];
      }

      if (address < 0xE000)
        return _memory[0x00, address - 0xC000];

      return _memory[0x00, address - 0xE000];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(ushort address, byte value)
    {
      if (address < 0x8000)
        return;

      if (address < 0xC000)
      {
        if (!_ramEnable)
          return;

        _memory[_ramPage, address - 0x8000] = value;
        return;
      }

      if (address < 0xE000)
      {
        _memory[0x00, address - 0xC000] = value;
        return;
      }

      if (address == 0xFFFC)
      {
        _ramEnable = value.GetBit(3);
        _ramPage = value.GetBit(2)
                 ? 0x02
                 : 0x01;
      }
      else if (address == 0xFFFD) _page0 = value;
      else if (address == 0xFFFE) _page1 = value;
      else if (address == 0xFFFF) _page2 = value;

      _memory[0x00, address - 0xE000] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadWord(ushort address)
    {
      var lowByte = ReadByte(address);
      var highByte = ReadByte(address.Increment());
      return highByte.Concat(lowByte);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteWord(ushort address, ushort word)
    {
      WriteByte(address, word.GetLowByte());
      WriteByte(address.Increment(), word.GetHighByte());
    }

    public void Dump(string path)
    {
      var dump = new List<string>();

      for (int bank = 0; bank < 0x03; bank++)
        for (byte hi = 0; hi < 0x40; hi++)
        {
          var row = string.Empty;
          for (byte lo = 0; lo < byte.MaxValue; lo++)
            row += _memory[bank, hi.Concat(lo)];
          dump.Add(row);
        }
          
      File.AppendAllLines(path, dump);
    }
  }
}