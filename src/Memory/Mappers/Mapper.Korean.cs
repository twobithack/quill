using System.Runtime.CompilerServices;

using Quill.Common.Extensions;
using Quill.Memory.Definitions;

namespace Quill.Memory;

unsafe public ref partial struct Mapper
{
  #region Constants
  private const ushort KOREAN_SLOT_SIZE     = BANK_SIZE * 2;
  private const ushort KOREAN_SLOT2_CONTROL = 0xA000;
  #endregion

  #region Methods
  private void InitializeSlotsKorean()
  {
    _slot2Control = 0x1;
    
    _slot0 = GetBank(0x0);
    _slot1 = GetBank(0x1);
    _slot2 = GetBank(0x2);
    _slot3 = GetBank(0x3);
    _vectors = _rom[..VECTORS_SIZE];
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void WriteByteKorean(ushort address, byte value)
  {
    if (address == KOREAN_SLOT2_CONTROL)
    {
      _slot2Control = value;
      RemapSlots();
    }
    else if (address >= BANK_SIZE * 3)
    {
      var index = address & (BANK_SIZE - 1);
      _ram[index] = value;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RemapSlotsKorean() => GetBankPair(_slot2Control, out _slot4, out _slot5);

  private static bool HasKnownKoreanHash(uint crc) => Hashes.Korean.Contains(crc);
  #endregion
}