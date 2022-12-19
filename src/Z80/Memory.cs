using System.Runtime.CompilerServices;
using Quill.Extensions;

namespace Quill
{
  public unsafe sealed class Memory
  {
    private const ushort PageSize = 0x4000;
    private readonly byte[] _ram = new byte[PageSize];
    private readonly byte[] _ramBank0 = new byte[PageSize];
    private readonly byte[] _ramBank1 = new byte[PageSize];
    private readonly byte[][] _rom = new byte[0x40][];

    private byte _page0 = 0x00;
    private byte _page1 = 0x01;
    private byte _page2 = 0x02;
    private bool _bankEnable;
    private bool _bankSelect;

    public Memory() => Array.Fill(_rom, new byte[PageSize]);

    public void LoadROM(byte[] rom)
    {
      for (int i = 0; i < rom.Count(); i++)
        _rom[i / PageSize][i % PageSize] = rom[i];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte(ushort address)
    {
      var index = address % PageSize;

      if (address < 0x400)
        return _rom[0x00][index];

      if (address < PageSize)
        return _rom[_page0][index];

      if (address < PageSize * 2)
        return _rom[_page1][index];
        
      if (address < PageSize * 3)
      {
        if (_bankEnable)
          return _bankSelect
               ? _ramBank0[index]
               : _ramBank1[index];

        return _rom[_page2][index];
      }

      return _ram[index];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(ushort address, byte value)
    {
      var index = address % PageSize;

      if (address < PageSize * 2)
        return;

      if (address > 0xDFFB && 
          address < 0xF000)
        return;

      if (address < PageSize * 3)
      {
        if (!_bankEnable)
          return;

        if (_bankSelect)
          _ramBank0[index] = value;
        else
          _ramBank1[index] = value;

        return;
      }

      if (address == 0xFFFC)
      {
        _bankEnable = value.TestBit(3);
        _bankSelect = value.TestBit(2);
      }
      else if (address == 0xFFFD) _page0 = value;
      else if (address == 0xFFFE) _page1 = value;
      else if (address == 0xFFFF) _page2 = value;

      _ram[index] = value;
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
      for (byte hi = 0; hi < 0x40; hi++)
      {
        var row = string.Empty;
        for (byte lo = 0; lo < byte.MaxValue; lo++)
          row += _ram[hi.Concat(lo)].ToHex() + " ";
        dump.Add(row);
      }
      File.WriteAllLines(path, dump);
    }

    public void DumpROM(string path)
    {
      var dump = new List<string>();
      for (byte page = 0; page < 0x40; page++)
        for (byte hi = 0; hi < 0x40; hi++)
        {
          var row = string.Empty;
          for (byte lo = 0; lo < byte.MaxValue; lo++)
            row += _rom[page][hi.Concat(lo)].ToHex();
          dump.Add(row);
        }
      File.WriteAllLines(path, dump);
    }

    public override string ToString()
    {
      var banking = (_bankEnable 
                  ? $"enabled (Bank {_bankSelect.ToBit()})"
                  : "disabled");
      return $"Memory: RAM banking {banking}";
    }
  }
}