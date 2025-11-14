using System.Runtime.CompilerServices;

using Quill.Memory.Definitions;

namespace Quill.Memory;

public ref partial struct Mapper
{
  #region Constants
  private const ushort MSX_SLOT0_CONTROL = 0x0002;
  private const ushort MSX_SLOT1_CONTROL = 0x0003;
  private const ushort MSX_SLOT2_CONTROL = 0x0000;
  private const ushort MSX_SLOT3_CONTROL = 0x0001;
  #endregion

  #region Methods
  private void InitializeSlotsMSX()
  {
    _slotControl0 = 0x0;
    _slotControl1 = 0x1;
    _slotControl2 = 0x2;
    _slotControl3 = 0x3;

    var bank0 = UseNemesisMapper()
              ? (byte)0xF
              : (byte)0x0;
    _slot0 = GetBank(bank0);
    _slot1 = GetBank(0x1);
    _vectors = _rom[..VECTORS_SIZE];
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void WriteByteMSX(ushort address, byte value)
  {
    if (address >= RAM_BASE)
    {
      WriteRAM(address, value);
    }
    else if (address == MSX_SLOT0_CONTROL)
    {
      _slotControl0 = value;
      RemapSlotsMSX();
    }
    else if (address == MSX_SLOT1_CONTROL)
    {
      _slotControl1 = value;
      RemapSlotsMSX();
    }
    else if (address == MSX_SLOT2_CONTROL)
    {
      _slotControl2 = value;
      RemapSlotsMSX();
    }
    else if (address == MSX_SLOT3_CONTROL)
    {
      _slotControl3 = value;
      RemapSlotsMSX();
    }
  }

  private void RemapSlotsMSX()
  {
    _slot2 = GetBank(_slotControl0);
    _slot3 = GetBank(_slotControl1);
    _slot4 = GetBank(_slotControl2);
    _slot5 = GetBank(_slotControl3);
  }

  private readonly bool UseNemesisMapper() => Hashes.Nemesis == GetCRC32Hash(_rom);
  private static bool HasKnownMSXHash(uint crc) => Hashes.MSX.Contains(crc);
  #endregion
}