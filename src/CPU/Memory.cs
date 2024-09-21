using CommunityToolkit.HighPerformance;
using Quill.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Quill.CPU;

unsafe public ref struct Memory
{
  #region Constants
  private const ushort PAGE_SIZE = 0x4000;
  #endregion

  #region Fields
  private readonly ReadOnlySpan2D<byte> _rom;
  private readonly Span<byte> _ram;
  private readonly Span<byte> _bank0;
  private readonly Span<byte> _bank1;
  private readonly bool _useMapper;
  private bool _bankEnable;
  private bool _bankSelect;
  private byte _page0;
  private byte _page1;
  private byte _page2;
  #endregion

  public Memory(byte[] program)
  {
    var headerOffset = (program.Length % PAGE_SIZE == 512) ? 512 : 0;
    var romSize = program.Length - headerOffset;
    var pages = (romSize - 1) / PAGE_SIZE;
    if (pages++ > byte.MaxValue) 
      throw new Exception("ROM too large for standard mapper.");

    var rom = new byte[pages, PAGE_SIZE];
    for (int i = headerOffset; i < program.Length; i++)
    {
      var page = i / PAGE_SIZE;
      var index = i % PAGE_SIZE;
      rom[page, index] = program[i];
    }

    _rom = new ReadOnlySpan2D<byte>(rom);
    _useMapper = pages > 3;
    _page0 = 0x00;
    _page1 = 0x01;
    _page2 = 0x02;

    _ram = new Span<byte>(new byte[PAGE_SIZE]);
    _bank0 = new Span<byte>(new byte[PAGE_SIZE]);
    _bank1 = new Span<byte>(new byte[PAGE_SIZE]);
    _bankEnable = false;
    _bankSelect = false;
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
  
    if (_useMapper &&
        address > 0xDFFB && 
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
      if (address % 64 == 0)
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
    var dump = new List<string>();
    for (byte page = 0; page < 0x40; page++)
    {
      var row = $"PAGE {page.ToHex()}";
      for (ushort index = 0; index < PAGE_SIZE; index++)
      {
        if (index % 16 == 0)
        {
          dump.Add(row);
          row = $"{index.ToHex()} : ";
        }
        row += _rom[page,index].ToHex();
      }
    }
    File.WriteAllLines(path, dump);
  }

  public override string ToString()
  {
    var banking = (_bankEnable ? $"enabled (Bank {_bankSelect.ToBit()})" : "disabled");
    return $"Memory: RAM banking {banking} | P0: {_page0.ToHex()}, P1: {_page1.ToHex()}, P2: {_page2.ToHex()}";
  }
}