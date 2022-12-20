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
    private byte[][] _rom = new byte[0x40][];

    private byte _page0;
    private byte _page1;
    private byte _page2;
    private bool _bankEnable;
    private bool _bankSelect;

    private int _reads;
    private int _writes;

    public Memory()
    {
      for (int i = 0; i < 0x40; i++)
        _rom[i] = new byte[PageSize];
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
      _reads++;

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
      // Console.WriteLine($"Writing {value.ToHex()} to {address.ToHex()}");

      if (address < PageSize * 2)
      {
        return;
      }

      if (address > 0xDFFB && 
          address < 0xF000)
        return;

      _writes++;
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
          temp1 += _ramBank0[hi.Concat(lo)].ToHex();
          temp2 += _ramBank1[hi.Concat(lo)].ToHex();
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
             $"{_reads} reads, {_writes} writes | " +
             $"P0: {_page0.ToHex()}, P1: {_page1.ToHex()}, P2: {_page2.ToHex()}";
    }
  }
}