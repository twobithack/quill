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
  #endregion

  public Mapper(byte[] program)
  {
    var headerOffset = HasHeader(program)
                     ? HEADER_SIZE
                     : 0;
    var romLength = program.Length - headerOffset;
    _bankCount = (romLength + BANK_SIZE - 1) / BANK_SIZE;
    _bankMask = GetBankMask(_bankCount);
    _mapper = DetectMapperType(program);

    var romPadded = new byte[_bankCount * BANK_SIZE];
    program.AsSpan(headerOffset, romLength).CopyTo(romPadded);

    _rom   = romPadded;
    _ram   = new byte[BANK_SIZE];
    _sram0 = new byte[BANK_SIZE];
    _sram1 = new byte[BANK_SIZE];
    _sram  = _sram0;

    InitializeSlots();
    _vectors = _mapper == MapperType.SEGA
             ? _rom[..BANKING_START]
             : _slot0[..BANKING_START];
  }

  #region Methods
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public readonly byte ReadByte(ushort address)
  {
    if (address < BANKING_START)
      return _vectors[address];

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

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void WriteByte(ushort address, byte value)
  {
    switch (_mapper)
    {
      case MapperType.SEGA:
        HandleWriteSEGA(address, value);
        return;

      case MapperType.Codemasters:
        HandleWriteCodemasters(address, value);
        return;

      case MapperType.Korean:
        HandleWriteKorean(address, value);
        return;

      case MapperType.MSX:
        HandleWriteMSX(address, value);
        return;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public readonly ushort ReadWord(ushort address)
  {
    var lowByte  = ReadByte(address);
    var highByte = ReadByte(address.Increment());
    return highByte.Concat(lowByte);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void WriteWord(ushort address, ushort word)
  {
    WriteByte(address, word.LowByte());
    WriteByte(address.Increment(), word.HighByte());
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void UpdateMappings()
  {
    switch (_mapper)
    {
      case MapperType.SEGA:
        UpdateMappingsSEGA();
        return;

      case MapperType.Codemasters:
        UpdateMappingsCodemasters();
        return;

      case MapperType.Korean:
        UpdateMappingsKorean();
        return;

      case MapperType.MSX:
        UpdateMappingsMSX();
        return;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private readonly ReadOnlySpan<byte> GetBank(byte controlByte)
  {
    var bank = controlByte % _bankCount;
    return _rom.Slice(bank * BANK_SIZE, BANK_SIZE);
  }

  private void InitializeSlots()
  {
    switch (_mapper)
    {
      case MapperType.SEGA:
        InitializeSlotsSEGA();
        break;

      case MapperType.Codemasters:
        InitializeSlotsCodemasters();
        break;

      case MapperType.Korean:
        InitializeSlotsKorean();
        break;

      case MapperType.MSX:
        InitializeSlotsMSX();
        break;
    }
    UpdateMappings();
  }

  private static byte GetBankMask(int bankCount)
  {
    return bankCount switch
    {
      <= 1   =>  0b_0000_0000,
      <= 2   =>  0b_0000_0001,
      <= 4   =>  0b_0000_0011,
      <= 8   =>  0b_0000_0111,
      <= 16  =>  0b_0000_1111,
      <= 32  =>  0b_0001_1111,
      <= 64  =>  0b_0011_1111,
      <= 128 =>  0b_0111_1111,
      _      =>  0b_1111_1111
    };
  }
  
  private static MapperType DetectMapperType(byte[] rom)
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

  private static uint GetCRC32Hash(byte[] rom)
  {
    var headerOffset = HasHeader(rom) ? HEADER_SIZE : 0;
    var hash = Crc32.Hash(rom.AsSpan(headerOffset, rom.Length - headerOffset));
    return BitConverter.ToUInt32(hash);
  }
  #endregion
}