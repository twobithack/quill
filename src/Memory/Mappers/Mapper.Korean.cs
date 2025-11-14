using System.Runtime.CompilerServices;

using Quill.Memory.Definitions;

namespace Quill.Memory;

public ref partial struct Mapper
{
  #region Constants
  private const ushort KOREAN_SLOT2_CONTROL = 0xA000;
  #endregion

  #region Methods
  private void InitializeSlotsKorean()
  {
    _slotControl2 = 0x1;
    
    _slot0 = GetBank(0x0);
    _slot1 = GetBank(0x1);
    _slot2 = GetBank(0x2);
    _slot3 = GetBank(0x3);
    _vectors = _rom[..VECTORS_SIZE];
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void WriteByteKorean(ushort address, byte value)
  {
    if (address >= RAM_BASE)
    {
      WriteRAM(address, value);
    }
    else if (address == KOREAN_SLOT2_CONTROL)
    {
      _slotControl2 = value;
      RemapSlotsKorean();
    }
  }

  private void RemapSlotsKorean() => GetBankPair(_slotControl2, out _slot4, out _slot5);

  private static bool HasKnownKoreanHash(uint crc) => Hashes.Korean.Contains(crc);
  #endregion
}