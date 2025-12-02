using System;
using System.Runtime.CompilerServices;

using Quill.Common.Extensions;
using Quill.Memory.Definitions;

namespace Quill.Memory;

public ref partial struct MemoryMap
{
  #region Constants
  private const ushort JANGGUN_SLOT0_CONTROL = 0x4000;
  private const ushort JANGGUN_SLOT1_CONTROL = 0x6000;
  private const ushort JANGGUN_SLOT2_CONTROL = 0x8000;
  private const ushort JANGGUN_SLOT3_CONTROL = 0xA000;
  private const ushort JANGGUN_SLOT4_CONTROL = 0xFFFE;
  private const ushort JANGGUN_SLOT5_CONTROL = 0xFFFF;
  #endregion

  private ReadOnlySpan<byte> _reversed;

  #region Methods
  private void InitializeMapperJanggun() => AllocateReversedBytes();

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void WriteByteJanggun(ushort address, byte value)
  {
    if (address == JANGGUN_SLOT4_CONTROL)
    {
      SetSlotPair(ref _slotControl0, ref _slotControl1, value);
      RemapSlotsJanggun();
    }
    else if (address == JANGGUN_SLOT5_CONTROL)
    {
      SetSlotPair(ref _slotControl2, ref _slotControl3, value);
      RemapSlotsJanggun();
    }
    
    if (address >= WRAM_BASE)
    {
      WriteWRAM(address, value);
      return;
    }

    if (address == JANGGUN_SLOT0_CONTROL)
    {
      SetSlot(ref _slotControl0, value);
      RemapSlotsJanggun();
    }
    else if (address == JANGGUN_SLOT1_CONTROL)
    {
      SetSlot(ref _slotControl1, value);
      RemapSlotsJanggun();
    }
    else if (address == JANGGUN_SLOT2_CONTROL)
    {
      SetSlot(ref _slotControl2, value);
      RemapSlotsJanggun();
    }
    else if (address == JANGGUN_SLOT3_CONTROL)
    {
      SetSlot(ref _slotControl3, value);
      RemapSlotsJanggun();
    }
  }

  private void RemapSlotsJanggun()
  {
    _vectors = _rom[..VECTORS_SIZE];
    _slot0 = GetBank(0x00);
    _slot1 = GetBank(0x01);
    _slot2 = GetBankJanggun(_slotControl0);
    _slot3 = GetBankJanggun(_slotControl1);
    _slot4 = GetBankJanggun(_slotControl2);
    _slot5 = GetBankJanggun(_slotControl3);
  }

  private readonly ReadOnlySpan<byte> GetBankJanggun(byte controlByte)
  {
    var index = controlByte & _romBankMask;
    return ReverseFlagSet(controlByte)
         ? _reversed.Slice(index * BANK_SIZE, BANK_SIZE)
         : _rom.Slice(index * BANK_SIZE, BANK_SIZE);
  }

  private readonly void SetSlot(ref byte slot, byte value) => slot = ReverseFlagSet(slot)
                                                                   ? value.SetBit(6)
                                                                   : value.ResetBit(6);

  private readonly void SetSlotPair(ref byte lowSlot, ref byte highSlot, byte value)
  {
    var reverseFlag = value & 0b_0100_0000;
    var lowIndex = (byte)((value << 1) & _romBankMask);
    lowSlot  = (byte)(lowIndex             | reverseFlag);
    highSlot = (byte)(lowIndex.Increment() | reverseFlag);
  }

  private void AllocateReversedBytes()
  {
    var reversed = new byte[_rom.Length];
    for (var index = 0; index < _rom.Length; index++)
      reversed[index] = ReverseByte(_rom[index]);
    _reversed = reversed;
  }

  private static byte ReverseByte(byte value)
  {
    value = (byte)((value >> 1) & 0b_0101_0101 | (value & 0b_0101_0101) << 1);
    value = (byte)((value >> 2) & 0b_0011_0011 | (value & 0b_0011_0011) << 2);
    value = (byte)((value >> 4) & 0b_0000_1111 | (value & 0b_0000_1111) << 4);
    return value;
  }

  private static bool ReverseFlagSet(byte value) => value.TestBit(6);
  private static bool HasJanggunHash(uint crc) => Hashes.Janggun == crc;
  #endregion
}