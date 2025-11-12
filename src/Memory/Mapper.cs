using System;
using System.IO.Hashing;
using System.Runtime.CompilerServices;

using Quill.Common.Extensions;
using Quill.Memory.Definitions;

namespace Quill.Memory;

unsafe public ref partial struct Mapper
{
  #region Constants
  public const ushort RAM_SIZE       = 0x2000;
  public const ushort BANK_SIZE      = 0x4000;
  
  private const ushort HEADER_SIZE   = 0x0200;
  private const ushort BANKING_START = 0x0400;
  private const ushort SRAM_CONTROL  = 0xFFFC;
  private const ushort SLOT0_CONTROL = 0xFFFD;
  private const ushort SLOT1_CONTROL = 0xFFFE;
  private const ushort SLOT2_CONTROL = 0xFFFF;
  #endregion

  public Mapper(byte[] program)
  {
    var headerOffset = HasHeader(program)
                     ? HEADER_SIZE
                     : 0;
    var romLength = program.Length - headerOffset;
    _bankCount = (romLength + BANK_SIZE - 1) / BANK_SIZE;
    _bankMask = CalculateBankMask(_bankCount);
    _mapper = GetMapperType(program);

    var romPadded = new byte[_bankCount * BANK_SIZE];
    program.AsSpan(headerOffset, romLength).CopyTo(romPadded);

    _rom   = romPadded;
    _ram   = new byte[BANK_SIZE];
    _sram0 = new byte[BANK_SIZE];
    _sram1 = new byte[BANK_SIZE];
    _sram  = _sram0;

    InitializeSlots();
    _fixed = _mapper == MapperType.SEGA
           ? _rom[..BANKING_START]
           : _slot0[..BANKING_START];
  }

  #region Methods
  public readonly byte ReadByte(ushort address)
  {
    if (address < BANKING_START)
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
    if (_mapper == MapperType.SEGA)
    {
      if (address < BANK_SIZE * 2)
        return;

      if (address == SRAM_CONTROL)
      {
        _sramEnable = value.TestBit(3);
        _sramSelect = value.TestBit(2);
        UpdateMappings();
      }
      else if (address == SLOT0_CONTROL)
      {
        _slot0Control = (byte)(value & _bankMask);
        UpdateMappings();
      }
      else if (address == SLOT1_CONTROL)
      {
        _slot1Control = (byte)(value & _bankMask);
        UpdateMappings();
      }
      else if (address == SLOT2_CONTROL)
      {
        _slot2Control = (byte)(value & _bankMask);
        UpdateMappings();
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
    else if (_mapper == MapperType.Codemasters)
    {
      if (address < BANK_SIZE)
      {
        _slot0Control = (byte)(value & _bankMask);
        UpdateMappings();
      }
      else if (address < BANK_SIZE * 2)
      {
        _slot1Control = (byte)(value & _bankMask);
        UpdateMappings();
      }
      else if (address < BANK_SIZE * 3)
      {
        _slot2Control = (byte)(value & _bankMask);
        UpdateMappings();
      }
      else
      {
        var index = address & (RAM_SIZE - 1);
        _ram[index] = value;
      }
    }
    else if (_mapper == MapperType.Korean)
    {
      if (address == 0xA000)
      {
        _slot2Control = (byte)(value & _bankMask);
        UpdateMappings();
      }
      else if (address >= BANK_SIZE * 3)
      {
        var index = address & (RAM_SIZE - 1);
        _ram[index] = value;
      }
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
    WriteByte(address, word.LowByte());
    WriteByte(address.Increment(), word.HighByte());
  }
  
  private void InitializeSlots()
  {
    _slot0Control = 0x00;
    _slot1Control = 0x01;
    _slot2Control = _mapper == MapperType.SEGA
                  ? (byte)0x2
                  : (byte)0x1;
    UpdateMappings();
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void UpdateMappings()
  {
    if (_mapper == MapperType.SEGA)
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
    else if (_mapper == MapperType.Codemasters)
    {
      _slot0 = GetBank(_slot0Control);
      _slot1 = GetBank(_slot1Control);
      _slot2 = GetBank(_slot2Control);
    }
    else if (_mapper == MapperType.Korean)
    {
      _slot0 = GetBank(0);
      _slot1 = GetBank(1);
      _slot2 = GetBank(_slot2Control);
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private readonly ReadOnlySpan<byte> GetBank(byte controlByte)
  {
    var bank = controlByte % _bankCount;
    return _rom.Slice(bank * BANK_SIZE, BANK_SIZE);
  }

  private static byte CalculateBankMask(int bankCount)
  {
    return bankCount switch
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
  }
  
  private static MapperType GetMapperType(byte[] rom)
  {
    if (rom.Length < 0x8000)
      return MapperType.SEGA;

    if (HasCodemastersHeader(rom))
      return MapperType.Codemasters;

    var hash = GetCRC32Hash(rom);

    if (HasKnownKoreanHash(hash))
      return MapperType.Korean;

    if (HasKnownMSXHash(hash))
      throw new Exception("MSX-style mapper not yet supported.");

    return MapperType.SEGA;
  }

  private static bool HasHeader(byte[] rom) => rom.Length % BANK_SIZE == HEADER_SIZE;

  private static bool HasCodemastersHeader(byte[] rom)
  {
    if (rom.Length < 0x7FEA)
      return false;

    var checksum = rom[0x7FE7].Concat(rom[0x7FE6]);
    if (checksum == 0x0)
      return false;

    var result = (ushort)(0x10000 - checksum);
    var answer = rom[0x7FE9].Concat(rom[0x7FE8]);
    return result == answer;
  }

  private static uint GetCRC32Hash(byte[] rom)
  {
    var headerOffset = HasHeader(rom) ? HEADER_SIZE : 0;
    var hash = Crc32.Hash(rom.AsSpan(headerOffset, rom.Length - headerOffset));
    return BitConverter.ToUInt32(hash);
  }

  private static bool HasKnownKoreanHash(uint crc) => Hashes.Korean.Contains(crc);

  private static bool HasKnownMSXHash(uint crc) => Hashes.MSX.Contains(crc);
  #endregion
}