using System.Runtime.CompilerServices;

using Quill.Common.Extensions;

namespace Quill.Memory;

public ref partial struct Mapper
{
  #region Constants
  private const ushort CODEMASTERS_SLOT_SIZE = BANK_SIZE * 2;
  #endregion

  #region Methods
  private void InitializeSlotsCodemasters()
  {
    _slotControl0 = 0x0;
    _slotControl1 = 0x1;
    _slotControl2 = 0x1;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void WriteByteCodemasters(ushort address, byte value)
  {
    if (address >= RAM_BASE)
    {
      WriteRAM(address, value);
    }
    else if (address < CODEMASTERS_SLOT_SIZE)
    {
      _slotControl0 = value;
      RemapSlotsCodemasters();
    }
    else if (address < CODEMASTERS_SLOT_SIZE * 2)
    {
      _slotControl1 = value;
      RemapSlotsCodemasters();
    }
    else if (address < CODEMASTERS_SLOT_SIZE * 3)
    {
      _slotControl2 = value;
      RemapSlotsCodemasters();
    }
  }

  private void RemapSlotsCodemasters()
  {
    GetBankPair(_slotControl0, out _slot0, out _slot1);
    GetBankPair(_slotControl1, out _slot2, out _slot3);
    GetBankPair(_slotControl2, out _slot4, out _slot5);
    
    _vectors = _slot0[..VECTORS_SIZE];
  }

  private static bool HasCodemastersHeader(byte[] rom)
  {
    if (rom.Length < 0x7FEA)
      return false;

    var checksum = rom[0x7FE7].Concat(rom[0x7FE6]);
    if (checksum == 0x0)
      return false;

    var result = (ushort)(0x10000 - checksum);
    var answer = rom[0x7FE9].Concat(rom[0x7FE8]);
    return result == answer;
  }
  #endregion
}