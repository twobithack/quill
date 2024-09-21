using CommunityToolkit.HighPerformance;
using Quill.Common.Extensions;
using Quill.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Quill.CPU;

unsafe public ref struct Memory
{
  #region Constants
  private const ushort HEADER_SIZE = 0x200;
  private const ushort PAGE_SIZE = 0x4000;
  private const ushort PAGING_START = 0x400;
  private const ushort MIRROR_SIZE = 0x2000;
  private const ushort MIRROR_START = 0xE000;
  private const ushort BANK_CONTROL = 0xFFFC;
  private const ushort PAGE0_CONTROL = 0xFFFD;
  private const ushort PAGE1_CONTROL = 0xFFFE;
  private const ushort PAGE2_CONTROL = 0xFFFF;
  #endregion

  #region Fields
  private readonly ReadOnlySpan2D<byte> _rom;
  private readonly Span<byte> _ram;
  private readonly Span<byte> _ramBank0;
  private readonly Span<byte> _ramBank1;
  private readonly byte _pageMask;
  private bool _bankEnable;
  private bool _bankSelect;
  private byte _page0;
  private byte _page1;
  private byte _page2;
  #endregion

  public Memory(byte[] program)
  {
    var headerOffset = (program.Length % PAGE_SIZE == HEADER_SIZE) ? HEADER_SIZE : 0;
    var pageCount = (program.Length + PAGE_SIZE - 1) / PAGE_SIZE;
    var rom = new byte[pageCount, PAGE_SIZE];
                     
    for (int i = headerOffset; i < program.Length; i++)
    {
      var page = i / PAGE_SIZE;
      var index = i % PAGE_SIZE;
      rom[page, index] = program[i];
    }

    _rom = new ReadOnlySpan2D<byte>(rom);
    _page0 = 0x00;
    _page1 = 0x01;
    _page2 = 0x02;
    
    _pageMask = pageCount switch 
    {
      <= 4 =>   0b_0000_0011,
      <= 8 =>   0b_0000_0111,
      <= 16 =>  0b_0000_1111,
      <= 32 =>  0b_0001_1111,
      <= 64 =>  0b_0011_1111,
      <= 128 => 0b_0111_1111,
      _ =>      0b_1111_1111,
    };

    _ram = new Span<byte>(new byte[PAGE_SIZE]);
    _ramBank0 = new Span<byte>(new byte[PAGE_SIZE]);
    _ramBank1 = new Span<byte>(new byte[PAGE_SIZE]);
    _bankEnable = false;
    _bankSelect = false;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public byte ReadByte(ushort address)
  {
    if (address < PAGING_START)
      return _rom[0x00, address];

    if (address < PAGE_SIZE)
      return _rom[_page0, address];

    var index = address % PAGE_SIZE;

    if (address < PAGE_SIZE * 2)
      return _rom[_page1, index];
      
    if (address < PAGE_SIZE * 3)
    {
      if (_bankEnable)
        return _bankSelect
             ? _ramBank0[index]
             : _ramBank1[index];

      return _rom[_page2, index];
    }

    if (address >= MIRROR_START)
      index -= MIRROR_SIZE;

    return _ram[index];
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void WriteByte(ushort address, byte value)
  {
    if (address < PAGE_SIZE * 2)
      return;

    var index = address % PAGE_SIZE;

    if (address < PAGE_SIZE * 3)
    {
      if (!_bankEnable)
        return;

      if (_bankSelect)
        _ramBank0[index] = value;
      else
        _ramBank1[index] = value;

      return;
    }

    if (address >= MIRROR_START)
      index -= MIRROR_SIZE;

    if (address == BANK_CONTROL)
    {
      _bankEnable = value.TestBit(3);
      _bankSelect = value.TestBit(2);
    }
    else if (address == PAGE0_CONTROL)
      _page0 = (byte)(value & _pageMask);
    else if (address == PAGE1_CONTROL)
      _page1 = (byte)(value & _pageMask);
    else if (address == PAGE2_CONTROL)
      _page2 = (byte)(value & _pageMask);

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

  public void LoadState(Snapshot state)
  {
    for (var index = 0; index < PAGE_SIZE; index++)
    {
      _ram[index] = state.RAM[index];
      _ramBank0[index] = state.Bank0[index];
      _ramBank1[index] = state.Bank1[index];
    }
    _page0 = state.Page0;
    _page1 = state.Page1;
    _page2 = state.Page2;
    _bankEnable = state.BankEnable;
    _bankSelect = state.BankSelect;
  }

  public void SaveState(ref Snapshot state)
  {
    _ram.CopyTo(state.RAM);
    _ramBank0.CopyTo(state.Bank0);
    _ramBank1.CopyTo(state.Bank1);
    state.Page0 = _page0;
    state.Page1 = _page1;
    state.Page2 = _page2;
    state.BankEnable= _bankEnable;
    state.BankSelect= _bankSelect;
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