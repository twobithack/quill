using System;
using System.IO.Hashing;
using System.Runtime.CompilerServices;

using Quill.Common.Extensions;
using Quill.Memory.Definitions;

namespace Quill.Memory;

unsafe public ref partial struct Mapper
{
  #region Constants
  public const ushort BANK_SIZE     = 0x2000;

  private const ushort HEADER_SIZE  = 0x0200;
  private const ushort VECTORS_SIZE = 0x0400;
  private const ushort RAM_BASE     = 0xC000;
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
  }

  #region Methods
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public readonly byte ReadByte(ushort address)
  {
    if (address < VECTORS_SIZE)
      return _vectors[address];

    if (address < BANK_SIZE)
      return _slot0[address];

    var index = address & (BANK_SIZE - 1);

    if (address < BANK_SIZE * 2)
      return _slot1[index];

    if (address < BANK_SIZE * 3)
      return _slot2[index];

    if (address < BANK_SIZE * 4)
      return _slot3[index];

    if (address < BANK_SIZE * 5)
      return _slot4[index];

    if (address < BANK_SIZE * 6)
      return _slot5[index];

    index &= BANK_SIZE - 1;
    return _ram[index];
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void WriteByte(ushort address, byte value)
  {
    switch (_mapper)
    {
      case MapperType.SEGA:
        WriteByteSEGA(address, value);
        return;

      case MapperType.Codemasters:
        WriteByteCodemasters(address, value);
        return;

      case MapperType.Korean:
        WriteByteKorean(address, value);
        return;

      case MapperType.MSX:
        WriteByteMSX(address, value);
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
  private void UpdateSlots()
  {
    switch (_mapper)
    {
      case MapperType.SEGA:
        RemapSlotsSEGA();
        return;

      case MapperType.Codemasters:
        RemapSlotsCodemasters();
        return;

      case MapperType.Korean:
        RemapSlotsKorean();
        return;

      case MapperType.MSX:
        RemapSlotsMSX();
        return;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private readonly ReadOnlySpan<byte> GetBank(byte bank)
  {
    var mirrored = bank % _bankCount;
    return _rom.Slice(mirrored * BANK_SIZE, BANK_SIZE);
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

    UpdateSlots();
    _vectors = _mapper == MapperType.SEGA
             ? _rom[..VECTORS_SIZE]
             : _slot0[..VECTORS_SIZE];
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
      return MapperType.MSX;

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