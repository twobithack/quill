using System;
using System.IO.Hashing;
using System.Runtime.CompilerServices;

using Quill.Common.Extensions;
using Quill.Memory.Definitions;

namespace Quill.Memory;

public ref partial struct MemoryMap
{
  #region Constants
  public const ushort WRAM_SIZE     = 0x2000;
  public const ushort SRAM_SIZE     = 0x4000;

  private const ushort BANK_SIZE    = 0x2000;
  private const ushort HEADER_SIZE  = 0x0200;
  private const ushort VECTORS_SIZE = 0x0400;
  private const ushort WRAM_BASE    = 0xC000;
  #endregion

  public MemoryMap(byte[] bios, byte[] program)
  {
    _bios = PadToBankSize(bios);
    if (!_bios.IsEmpty)
    {
      _biosLoaded = true;
      _biosBankCount = _bios.Length / BANK_SIZE;
      _biosBankMask = GetBankMask(_biosBankCount);
    }

    _rom = PadToBankSize(program);
    _romBankCount = _rom.Length / BANK_SIZE;
    _romBankMask = GetBankMask(_romBankCount);

    _wram  = new byte[WRAM_SIZE];
    _sram0 = new byte[SRAM_SIZE];
    _sram1 = new byte[SRAM_SIZE];
    _sram  = _sram0;
    
    _cartridgeMapper = DetectMapperType(program);
    InitializeMapper();
    RemapSlots();
  }

  #region Methods
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public readonly byte ReadByte(ushort address)
  {
    if (address < VECTORS_SIZE)
      return _vectors[address];

    var slot  = address >> 13;
    var index = address & (BANK_SIZE - 1);

    return slot switch
    {
      0 => _slot0[index], // 0x0000 - 0x1FFF
      1 => _slot1[index], // 0x2000 - 0x3FFF
      2 => _slot2[index], // 0x4000 - 0x5FFF
      3 => _slot3[index], // 0x6000 - 0x7FFF
      4 => _slot4[index], // 0x8000 - 0x9FFF
      5 => _slot5[index], // 0xA000 - 0xBFFF
      _ => _slot6[index]  // 0xC000 - 0xDFFF (or 0xE000 - 0xFFFF mirror)
    };
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void WriteByte(ushort address, byte value)
  {
    if (EnableBIOS)
    {
      WriteByteBIOS(address, value);
      return;
    }

    switch (_cartridgeMapper)
    {
      case MapperType.Sega:        WriteByteSega(address, value);        return;
      case MapperType.Codemasters: WriteByteCodemasters(address, value); return;
      case MapperType.Korean:      WriteByteKorean(address, value);      return;
      case MapperType.MSX:         WriteByteMSX(address, value);         return;
      case MapperType.Janggun:     WriteByteJanggun(address, value);     return;
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
    WriteByte(address,             word.LowByte());
    WriteByte(address.Increment(), word.HighByte());
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void WriteControl(byte value)
  {
    var biosToggled = value.TestBit(3) ^ _memoryControl.TestBit(3);
    _memoryControl = value;

    if (biosToggled && _biosLoaded)
      InitializeMapper();
    
    RemapSlots();
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private readonly void WriteWRAM(ushort address, byte value)
  {
    if (!EnableWRAM)
      return;

    var index = address & (BANK_SIZE - 1);
    _wram[index] = value;
  }

  private void InitializeMapper()
  {
    if (EnableBIOS)
    {
      InitializeMapperBIOS();
      return;
    }

    switch (_cartridgeMapper)
    {
      case MapperType.Sega:        InitializeMapperSega();        break;
      case MapperType.Codemasters: InitializeMapperCodemasters(); break;
      case MapperType.Korean:      InitializeMapperKorean();      break;
      case MapperType.MSX:         InitializeMapperMSX();         break;
      case MapperType.Janggun:     InitializeMapperJanggun();     break;
    }
  }

  private void RemapSlots()
  {
    _slot6 = EnableWRAM
           ? _wram
           : _unmapped;

    if (EnableBIOS)
    {
      RemapSlotsBIOS();
      return;
    }
    else if (!EnableCartridge)
    {
      UnmapSlots();
      return;
    }
    
    switch (_cartridgeMapper)
    {
      case MapperType.Sega:        RemapSlotsSega();        return;
      case MapperType.Codemasters: RemapSlotsCodemasters(); return;
      case MapperType.Korean:      RemapSlotsKorean();      return;
      case MapperType.MSX:         RemapSlotsMSX();         return;
      case MapperType.Janggun:     RemapSlotsJanggun();     return;
    }
  }

  private void UnmapSlots()
  {
    _vectors = _unmapped[..VECTORS_SIZE];
    _slot0   = _unmapped;
    _slot1   = _unmapped;
    _slot2   = _unmapped;
    _slot3   = _unmapped;
    _slot4   = _unmapped;
    _slot5   = _unmapped;
  }

  private readonly ReadOnlySpan<byte> GetBank(byte controlByte)
  {
    var index = controlByte & _romBankMask;
    var mirrored = index % _romBankCount;
    return _rom.Slice(mirrored * BANK_SIZE, BANK_SIZE);
  }

  private readonly void GetBankPair(byte controlByte,
                                    out ReadOnlySpan<byte> lowBank,
                                    out ReadOnlySpan<byte> highBank)
  {
    var lowIndex = (byte)(controlByte << 1);
    lowBank  = GetBank(lowIndex);
    highBank = GetBank(lowIndex.Increment());
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
    if (rom.Length < BANK_SIZE * 4)
      return MapperType.Sega;

    if (HasCodemastersHeader(rom))
      return MapperType.Codemasters;

    var hash = GetCRC32Hash(rom);

    if (HasKnownKoreanHash(hash))
      return MapperType.Korean;

    if (HasKnownMSXHash(hash))
      return MapperType.MSX;

    if (HasJanggunHash(hash))
      return MapperType.Janggun;

    return MapperType.Sega;
  }

  private static int GetHeaderOffset(ReadOnlySpan<byte> rom)
  {
    var containsHeader = rom.Length % BANK_SIZE == HEADER_SIZE;
    return containsHeader
         ? HEADER_SIZE
         : 0;
  }

  private static uint GetCRC32Hash(ReadOnlySpan<byte> rom)
  {
    var headerOffset = GetHeaderOffset(rom);
    var hash = Crc32.Hash(rom[headerOffset..]);
    return BitConverter.ToUInt32(hash);
  }

  private static ReadOnlySpan<byte> PadToBankSize(byte[] rom)
  {
    var remainder = rom.Length % BANK_SIZE;
    if (remainder == 0)
      return rom;

    var padding = BANK_SIZE - remainder;
    var paddedSize = rom.Length + padding;

    Array.Resize(ref rom, paddedSize);
    return rom.AsSpan();
  }
  #endregion
}