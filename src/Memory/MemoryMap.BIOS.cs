using System;
using System.Runtime.CompilerServices;

namespace Quill.Memory;

public ref partial struct MemoryMap
{
  #region Methods
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void WriteByteBIOS(ushort address, byte value)
  {
    if (address >= WRAM_BASE)
      WriteWRAM(address, value);
  }

  private void RemapSlotsBIOS()
  {
    _vectors = _bios[..VECTORS_SIZE];
    _slot0 = GetBankBIOS(0x00);
    _slot1 = GetBankBIOS(0x01);
    _slot2 = GetBankBIOS(0x02);
    _slot3 = GetBankBIOS(0x03);
    _slot4 = GetBankBIOS(0x04);
    _slot5 = GetBankBIOS(0x05);
  }

  private ReadOnlySpan<byte> GetBankBIOS(byte bank)
  {
    var start = bank * BANK_SIZE;
    return start < _bios.Length
         ? _bios.Slice(start, BANK_SIZE)
         : _unmapped;
  }
  #endregion
}