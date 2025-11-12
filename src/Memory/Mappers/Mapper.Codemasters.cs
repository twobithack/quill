using System.Runtime.CompilerServices;

using Quill.Common.Extensions;

namespace Quill.Memory;

unsafe public ref partial struct Mapper
{
  #region Constants
  private const ushort CODEMASTERS_SLOT_SIZE = BANK_SIZE * 2;
  #endregion

  #region Methods
  private void InitializeSlotsCodemasters()
  {
    _slot0Control = 0x0;
    _slot1Control = 0x1;
    _slot2Control = 0x1;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void WriteByteCodemasters(ushort address, byte value)
  {
    if (address < CODEMASTERS_SLOT_SIZE)
    {
      _slot0Control = value;
      RemapSlotsCodemasters();
    }
    else if (address < CODEMASTERS_SLOT_SIZE * 2)
    {
      _slot1Control = value;
      RemapSlotsCodemasters();
    }
    else if (address < CODEMASTERS_SLOT_SIZE * 3)
    {
      _slot2Control = value;
      RemapSlotsCodemasters();
    }
    else
    {
      var index = address & (BANK_SIZE - 1);
      _ram[index] = value;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RemapSlotsCodemasters()
  {
    var bank0 = (byte)((_slot0Control << 1) & _bankMask);
    _slot0 = GetBank(bank0);
    _slot1 = GetBank(bank0.Increment());

    var bank1 = (byte)((_slot1Control << 1) & _bankMask);
    _slot2 = GetBank(bank1);
    _slot3 = GetBank(bank1.Increment());

    var bank2 = (byte)((_slot2Control << 1) & _bankMask);
    _slot4 = GetBank(bank2);
    _slot5 = GetBank(bank2.Increment());
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