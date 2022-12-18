using System.Runtime.CompilerServices;
using Quill.Extensions;

namespace Quill.Z80
{
  public unsafe sealed class Memory
  {
    private const ushort PageSize = 0x4000;
    private byte[,] _ram;
    private byte[,] _rom;
    private byte _page0;
    private byte _page1;
    private byte _page2;
    private bool _ramEnable;
    private int _ramPage;

    public Memory()
    {
      _ram = new byte[0x03, PageSize];
      _rom = new byte[0x40, PageSize];
    }

    public void LoadROM(byte[] rom)
    {
      for (int i = 0; i < rom.Count(); i++)
        _rom[i / PageSize, i % PageSize] = rom[i];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte(ushort address)
    {
      var index = address % PageSize;

      if (address < 0x400)
        return _rom[0x00, index];

      if (address < PageSize)
        return _rom[_page0, index];

      if (address < PageSize * 2)
        return _rom[_page1, index];
        
      if (address < PageSize * 3)
      {
        if (!_ramEnable)
          return _rom[_page2, index];

        return _ram[_ramPage, index];
      }

      return _ram[0x00, index];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(ushort address, byte value)
    {
      var index = address % PageSize;

      if (address < PageSize * 2)
        return;

      if (address > 0xDFFB && address < 0xF000)
        return;

      if (address < PageSize * 3)
      {
        if (!_ramEnable)
          return;

        _ram[_ramPage, index] = value;
        return;
      }

      if (address == 0xFFFC)
      {
        _ramEnable = value.TestBit(3);
        _ramPage = value.TestBit(2)
                 ? 0x02
                 : 0x01;
      }
      else if (address == 0xFFFD) _page0 = value;
      else if (address == 0xFFFE) _page1 = value;
      else if (address == 0xFFFF) _page2 = value;

      _ram[0x00, index] = value;
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
            row += _ram[bank, hi.Concat(lo)].ToHex() + " ";
          dump.Add(row);
        }
          
      File.WriteAllLines(path, dump);
    }
  }
}