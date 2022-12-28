using CommunityToolkit.HighPerformance;
using Quill.Common;
using System.Runtime.CompilerServices;

namespace Quill.CPU;

unsafe public ref struct Memory
{
  private const ushort PAGE_COUNT = 0x40;
  private const ushort PAGE_SIZE = 0x4000;

  private readonly Span<byte> _ram;
  private readonly Span<byte> _bank0;
  private readonly Span<byte> _bank1;
  private bool _bankEnable;
  private bool _bankSelect;

  private readonly ReadOnlySpan2D<byte> _rom;
  private byte _page0 = 0x00;
  private byte _page1 = 0x01;
  private byte _page2 = 0x02;

  public Memory(byte[] program)
  {
    var headerOffset = (program.Length % PAGE_SIZE == 512) ? 512 : 0;
    var rom = new byte[PAGE_COUNT, PAGE_SIZE];

    for (int i = 0; i < program.Length; i++)
    {
      var page = (i / PAGE_SIZE) + headerOffset;
      var index = (i % PAGE_SIZE) + headerOffset;
      rom[page, index] = program[i];
    }

    _rom = new ReadOnlySpan2D<byte>(rom);
    _ram = new Span<byte>(new byte[PAGE_SIZE]);
    _bank0 = new Span<byte>(new byte[PAGE_SIZE]);
    _bank1 = new Span<byte>(new byte[PAGE_SIZE]);
    _bankEnable = _bankSelect = false;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public byte ReadByte(ushort address)
  {
    var index = address % PAGE_SIZE;

    if (address < 0x400)
      return _rom[0x00, index];

    if (address < PAGE_SIZE)
      return _rom[_page0, index];

    if (address < PAGE_SIZE * 2)
      return _rom[_page1, index];
      
    if (address < PAGE_SIZE * 3)
    {
      if (_bankEnable)
        return _bankSelect
             ? _bank0[index]
             : _bank1[index];

      return _rom[_page2, index];
    }

    return _ram[index];
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void WriteByte(ushort address, byte value)
  {
    var index = address % PAGE_SIZE;

    if (address < PAGE_SIZE * 2)
      return;
  
    if (address > 0xDFFB && 
        address < 0xF000)
      return;

    if (address < PAGE_SIZE * 3)
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
    WriteByte(address, word.LowByte());
    WriteByte(address.Increment(), word.HighByte());
  }

  public void DumpRAM(string path)
  {
    var memory = new List<string>();
    var row = string.Empty;

    for (ushort address = 0; address < PAGE_SIZE; address++)
    {
      if (address % PAGE_COUNT == 0)
      {
        memory.Add(row);
        row = string.Empty;
      }
      row += _ram[address].ToHex();
    }
    
    File.WriteAllLines(path, memory);
  }

  public void DumpROM(string path)
  {
    Console.Write("Dumping ROM");
    var dump = new List<string>();
    for (byte page = 0; page < 0x40; page++)
    {
      for (byte hi = 0; hi < 0x40; hi++)
      {
        var row = string.Empty;
        for (byte lo = 0; lo < byte.MaxValue; lo++)
          row += _rom[page,hi.Concat(lo)].ToHex();
        dump.Add(row);
        Console.Write('.');
      }
    }
    File.WriteAllLines(path, dump);
    Console.WriteLine(" Done.");
  }

  public override string ToString()
  {
    var banking = (_bankEnable ? $"enabled (Bank {_bankSelect.ToBit()})" : "disabled");
    return $"Memory: RAM banking {banking} | P0: {_page0.ToHex()}, P1: {_page1.ToHex()}, P2: {_page2.ToHex()}";
  }
}