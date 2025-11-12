using System.Runtime.CompilerServices;

using Quill.Memory.Definitions;

namespace Quill.Memory;

unsafe public ref partial struct Mapper
{
  #region Constants
  private const ushort MSX_SLOT_SIZE     = BANK_SIZE;
  private const ushort MSX_SLOT0_CONTROL = 0x0002;
  private const ushort MSX_SLOT1_CONTROL = 0x0003;
  private const ushort MSX_SLOT2_CONTROL = 0x0000;
  private const ushort MSX_SLOT3_CONTROL = 0x0001;
  #endregion

  #region Methods
  private void InitializeSlotsMSX()
  {
    _slot0Control = 0x0;
    _slot1Control = 0x1;
    _slot2Control = 0x2;
    _slot3Control = 0x3;
  }

  private void WriteByteMSX(ushort address, byte value)
  {
    if (address == MSX_SLOT0_CONTROL)
    {
      _slot0Control = (byte)(value & _bankMask);
      RemapSlotsMSX();
    }
    else if (address == MSX_SLOT1_CONTROL)
    {
      _slot1Control = (byte)(value & _bankMask);
      RemapSlotsMSX();
    }
    else if (address == MSX_SLOT2_CONTROL)
    {
      _slot2Control = (byte)(value & _bankMask);
      RemapSlotsMSX();
    }
    else if (address == MSX_SLOT3_CONTROL)
    {
      _slot3Control = (byte)(value & _bankMask);
      RemapSlotsMSX();
    }
    else if (address >= RAM_BASE)
    {
      var index = address & (BANK_SIZE - 1);
      _ram[index] = value;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RemapSlotsMSX()
  {
    _slot0 = GetBank(0x0);
    _slot1 = GetBank(0x1);
    _slot2 = GetBank(_slot0Control);
    _slot3 = GetBank(_slot1Control);
    _slot4 = GetBank(_slot2Control);
    _slot5 = GetBank(_slot3Control);
  }

  private static bool HasKnownMSXHash(uint crc) => Hashes.MSX.Contains(crc);
  #endregion
}