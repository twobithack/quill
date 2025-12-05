using System.Runtime.CompilerServices;

using Quill.Memory.Definitions;

namespace Quill.Memory;

public ref partial struct MemoryMap
{
  #region Constants
  private const ushort KOREAN_SLOT_CONTROL = 0xA000;
  #endregion

  #region Methods
  private void InitializeMapperKorean() => _slotControl0 = 0x01;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void WriteByteKorean(ushort address, byte value)
  {
    if (address >= WRAM_BASE)
    {
      WriteWRAM(address, value);
    }
    else if (address == KOREAN_SLOT_CONTROL)
    {
      _slotControl0 = value;
      RemapSlotsKorean();
    }
  }

  private void RemapSlotsKorean()
  {
    _vectors = _rom[..VECTORS_SIZE];
    GetBankPair(0x00,          out _slot0, out _slot1);
    GetBankPair(0x01,          out _slot2, out _slot3);
    GetBankPair(_slotControl0, out _slot4, out _slot5);
  }

  private static bool HasKnownKoreanHash(uint crc) => Hashes.Korean.Contains(crc);
  #endregion
}