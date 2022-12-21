using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Quill.Extensions;

namespace Quill
{
  public unsafe struct Memory
  {
    private const ushort PageCount = 0x40;
    private const ushort PageSize = 0x4000;
    private readonly byte[] _ram;
    private readonly byte[] _bank0;
    private readonly byte[] _bank1;
    private readonly byte[][] _rom;

    private byte _page0 = 0x00;
    private byte _page1 = 0x01;
    private byte _page2 = 0x02;
    private bool _bankEnable;
    private bool _bankSelect;

    public Memory()
    {
      _bankEnable = _bankSelect = false;
      _ram = new byte[PageSize]; //GC.AllocateArray<byte>(PageSize, pinned: true);
      _bank0 = new byte[PageSize]; //GC.AllocateArray<byte>(PageSize, pinned: true);
      _bank1 = new byte[PageSize]; //GC.AllocateArray<byte>(PageSize, pinned: true);

      _rom = new byte[PageCount][];
      for (int i = 0; i < 0x40; i++)
        _rom[i] = new byte[PageSize]; //GC.AllocateArray<byte>(PageSize, pinned: true);
    }
    public void LoadROM(byte[] program)
    {
      var headerOffset = program.Length % PageSize;
      for (int i = 0; i < program.Length; i++)
      {
        var page = (i / PageSize) + headerOffset;
        var index = (i % PageSize) + headerOffset;
        _rom[page][index] = program[i];
      }
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
               ? _bank0[index]
               : _bank1[index];

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
          _bank0[index] = value;
        else
          _bank1[index] = value;

        return;
      }

      if (address == 0xFFFC)
      {
        _bankEnable = value.TestBit(3);
        _bankSelect = value.TestBit(2);
      }
      else if (address == 0xFFFD) _page0 = (byte)(value & 0b_0011_1111);
      else if (address == 0xFFFE) _page1 = (byte)(value & 0b_0011_1111);
      else if (address == 0xFFFF) _page2 = (byte)(value & 0b_0011_1111);

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

    public void DumpRAM(string path)
    {
      var memory = new List<string>();
      var bank0 = new List<string>();
      var bank1 = new List<string>();
      for (byte hi = 0; hi < 0x40; hi++)
      {
        var temp0 = string.Empty;
        var temp1 = string.Empty;
        var temp2 = string.Empty;
        for (byte lo = 0; lo < byte.MaxValue; lo++)
        {
          temp0 += _ram[hi.Concat(lo)].ToHex();
          temp1 += this._bank0[hi.Concat(lo)].ToHex();
          temp2 += _bank1[hi.Concat(lo)].ToHex();
        }
        memory.Add(temp0);
        bank0.Add(temp1);
        bank1.Add(temp2);
      }
      memory.AddRange(bank0);
      memory.AddRange(bank1);
      
      File.WriteAllLines(path, memory);
    }

    public void DumpROM(string path)
    {
      Console.Write("Dumping...");
      var dump = new List<string>();
      for (byte page = 0; page < 0x40; page++)
      {
        for (byte hi = 0; hi < 0x40; hi++)
        {
          var row = string.Empty;
          for (byte lo = 0; lo < byte.MaxValue; lo++)
            row += _rom[page][hi.Concat(lo)].ToHex();
          dump.Add(row);
        }
      }
      File.WriteAllLines(path, dump);
      Console.WriteLine(" Done.");
    }

    public override string ToString()
    {
      var banking = (_bankEnable 
                  ? $"enabled (Bank {_bankSelect.ToBit()})"
                  : "disabled");
      return $"Memory: RAM banking {banking} | " + 
             $"P0: {_page0.ToHex()}, P1: {_page1.ToHex()}, P2: {_page2.ToHex()}";
    }
  }
}