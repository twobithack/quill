using System.Runtime.CompilerServices;

using Quill.Memory.Definitions;

namespace Quill.Memory;

public ref partial struct MemoryMap
{
  #region Constants
  private const ushort MSX_SLOT0_CONTROL = 0x0002;
  private const ushort MSX_SLOT1_CONTROL = 0x0003;
  private const ushort MSX_SLOT2_CONTROL = 0x0000;
  private const ushort MSX_SLOT3_CONTROL = 0x0001;
  #endregion

  private bool _useNemesisMapper;

  #region Methods
  private void InitializeMapperMSX()
  {
    _useNemesisMapper = HasNemesisHash();
    _slotControl0 = 0x00;
    _slotControl1 = 0x01;
    _slotControl2 = 0x02;
    _slotControl3 = 0x03;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void WriteByteMSX(ushort address, byte value)
  {
    if (address >= WRAM_BASE)
    {
      WriteWRAM(address, value);
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
    var bank0 = _useNemesisMapper
              ? (byte)0x0F
              : (byte)0x00;

    _slot0 = GetBank(bank0);
    _slot1 = GetBank(0x01);
    _slot2 = GetBank(_slotControl0);
    _slot3 = GetBank(_slotControl1);
    _slot4 = GetBank(_slotControl2);
    _slot5 = GetBank(_slotControl3);
    _vectors = _slot0[..VECTORS_SIZE];
  }

  private readonly bool HasNemesisHash() => Hashes.Nemesis == GetCRC32Hash(_rom);
  private static bool HasKnownMSXHash(uint crc) => Hashes.MSX.Contains(crc);
  #endregion
}