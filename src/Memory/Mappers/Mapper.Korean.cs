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
    _slot0Control = 0x0;
    _slot1Control = 0x1;
    _slot2Control = 0x1;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void WriteByteKorean(ushort address, byte value)
  {
    if (address == KOREAN_SLOT2_CONTROL)
    {
      _slot2Control = value;
      UpdateSlots();
    }
    else if (address >= BANK_SIZE * 3)
    {
      var index = address & (BANK_SIZE - 1);
      _ram[index] = value;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RemapSlotsKorean()
  {
    _slot0 = GetBank(0x0);
    _slot1 = GetBank(0x1);
    _slot2 = GetBank(0x2);
    _slot3 = GetBank(0x3);

    var bank = (byte)((_slot2Control << 1) & _bankMask);
    _slot4 = GetBank(bank);
    _slot5 = GetBank(bank.Increment());
  }

  private static bool HasKnownKoreanHash(uint crc) => Hashes.Korean.Contains(crc);
  #endregion
}