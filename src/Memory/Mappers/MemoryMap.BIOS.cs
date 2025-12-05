using System;
using System.Runtime.CompilerServices;

using Quill.Common.Extensions;

namespace Quill.Memory;

public ref partial struct MemoryMap
{
  #region Methods
  private void InitializeMapperBIOS() => InitializeMapperSega();

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void WriteByteBIOS(ushort address, byte value)
  {
    if (address == SEGA_SLOT0_CONTROL)
    {
      _slotControl0 = value;
      RemapSlotsBIOS();
    }
    else if (address == SEGA_SLOT1_CONTROL)
    {
      _slotControl1 = value;
      RemapSlotsBIOS();
    }
    else if (address == SEGA_SLOT2_CONTROL)
    {
      _slotControl2 = value;
      RemapSlotsBIOS();
    }

    if (address >= WRAM_BASE)
    {
      WriteWRAM(address, value);
    }
  }

  private void RemapSlotsBIOS()
  {
    _vectors = _bios[..VECTORS_SIZE];
    GetBankPairBIOS(_slotControl0, out _slot0, out _slot1);
    GetBankPairBIOS(_slotControl1, out _slot2, out _slot3);
    GetBankPairBIOS(_slotControl2, out _slot4, out _slot5);
  }

  private readonly ReadOnlySpan<byte> GetBankBIOS(byte controlByte)
  {
    var index = controlByte & _biosBankMask;
    var mirrored = index % _biosBankCount;
    return _bios.Slice(mirrored * BANK_SIZE, BANK_SIZE);
  }

  private readonly void GetBankPairBIOS(byte controlByte,
                                        out ReadOnlySpan<byte> lowBank,
                                        out ReadOnlySpan<byte> highBank)
  {
    var lowIndex = (byte)(controlByte << 1);
    lowBank  = GetBankBIOS(lowIndex);
    highBank = GetBankBIOS(lowIndex.Increment());
  }
  #endregion
}