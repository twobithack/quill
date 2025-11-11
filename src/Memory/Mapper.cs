using System;
using System.Runtime.CompilerServices;
using Quill.Common.Extensions;

namespace Quill.Memory;

unsafe public ref partial struct Mapper
{
  #region Constants
  public const ushort RAM_SIZE       = 0x2000;
  public const ushort BANK_SIZE      = 0x4000;
  
  private const ushort HEADER_SIZE   = 0x0200;
  private const ushort PAGING_START  = 0x0400;
  private const ushort SRAM_CONTROL  = 0xFFFC;
  private const ushort SLOT0_CONTROL = 0xFFFD;
  private const ushort SLOT1_CONTROL = 0xFFFE;
  private const ushort SLOT2_CONTROL = 0xFFFF;
  #endregion

  public Mapper(byte[] program)
  {
    var headerOffset = (program.Length % BANK_SIZE == HEADER_SIZE) ? HEADER_SIZE : 0;
    var romLength = program.Length - headerOffset;
    _pageCount = (romLength + BANK_SIZE - 1) / BANK_SIZE;

    var romPadded = new byte[_pageCount * BANK_SIZE];
    program.AsSpan(headerOffset, romLength).CopyTo(romPadded);
    _rom = romPadded;
    _fixed = _rom[..PAGING_START];

    _ram   = new byte[BANK_SIZE];
    _sram0 = new byte[BANK_SIZE];
    _sram1 = new byte[BANK_SIZE];
    _sram  = _sram0;

    _pageMask = _pageCount switch
    {
      <= 1    =>  0b_0000_0000,
      <= 2    =>  0b_0000_0001,
      <= 4    =>  0b_0000_0011,
      <= 8    =>  0b_0000_0111,
      <= 16   =>  0b_0000_1111,
      <= 32   =>  0b_0001_1111,
      <= 64   =>  0b_0011_1111,
      <= 128  =>  0b_0111_1111,
      _       =>  0b_1111_1111
    };
    
    _slot0Control = 0x00;
    _slot1Control = 0x01;
    _slot2Control = 0x02;
    UpdateSlots();
  }

  #region Methods
  public readonly byte ReadByte(ushort address)
  {
    if (address < PAGING_START)
      return _fixed[address];

    if (address < BANK_SIZE)
      return _slot0[address];

    var index = address & (BANK_SIZE - 1);

    if (address < BANK_SIZE * 2)
      return _slot1[index];

    if (address < BANK_SIZE * 3)
      return _slot2[index];

    index &= RAM_SIZE - 1;
    return _ram[index];
  }

  public void WriteByte(ushort address, byte value)
  {
    if (address < BANK_SIZE * 2)
      return;

    if (address == SRAM_CONTROL)
    {
      _sramEnable = value.TestBit(3);
      _sramSelect = value.TestBit(2);
      UpdateSlots();
    }
    else if (address == SLOT0_CONTROL)
    {
      _slot0Control = (byte)(value & _pageMask);
      UpdateSlots();
    }
    else if (address == SLOT1_CONTROL)
    {
      _slot1Control = (byte)(value & _pageMask);
      UpdateSlots();
    }
    else if (address == SLOT2_CONTROL)
    {
      _slot2Control = (byte)(value & _pageMask);
      UpdateSlots();
    }

    if (address < BANK_SIZE * 3)
    {
      if (!_sramEnable)
        return;
      var index = address & (BANK_SIZE - 1);
      _sram[index] = value;
    }
    else
    {
      var index = address & (RAM_SIZE - 1);
      _ram[index] = value;
    }    
  }

  public readonly ushort ReadWord(ushort address)
  {
    var lowByte  = ReadByte(address);
    var highByte = ReadByte(address.Increment());
    return highByte.Concat(lowByte);
  }

  public void WriteWord(ushort address, ushort word)
  {
    WriteByte(address,             word.LowByte());
    WriteByte(address.Increment(), word.HighByte());
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void UpdateSlots()
  {
    _sram = _sramSelect
          ? _sram0
          : _sram1;

    _slot0 = GetBank(_slot0Control);
    _slot1 = GetBank(_slot1Control);
    _slot2 = _sramEnable
           ? _sram
           : GetBank(_slot2Control);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private readonly ReadOnlySpan<byte> GetBank(byte controlByte)
  {
    var pageIndex = controlByte & _pageMask;
    var wrapped = pageIndex % _pageCount;
    return _rom.Slice(wrapped * BANK_SIZE, BANK_SIZE);
  }
  #endregion
}