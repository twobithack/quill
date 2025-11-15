using System.Runtime.CompilerServices;

using Quill.Common.Extensions;

namespace Quill.Memory;

public ref partial struct Mapper
{
  #region Constants
  private const ushort SEGA_SLOT_SIZE = BANK_SIZE * 2;
  private const ushort SRAM_CONTROL   = 0xFFFC;
  private const ushort SLOT0_CONTROL  = 0xFFFD;
  private const ushort SLOT1_CONTROL  = 0xFFFE;
  private const ushort SLOT2_CONTROL  = 0xFFFF;
  #endregion
  
  #region Methods
  private void InitializeSlotsSEGA()
  {
    _slotControl0 = 0x0;
    _slotControl1 = 0x1;
    _slotControl2 = 0x2;
    
    _vectors = _rom[..VECTORS_SIZE];
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void WriteByteSEGA(ushort address, byte value)
  {
    if (address == SRAM_CONTROL)
    {
      _sramEnable = value.TestBit(3);
      _sramSelect = value.TestBit(2);
      RemapSlotsSEGA();
    }
    else if (address == SLOT0_CONTROL)
    {
      _slotControl0 = value;
      RemapSlotsSEGA();
    }
    else if (address == SLOT1_CONTROL)
    {
      _slotControl1 = value;
      RemapSlotsSEGA();
    }
    else if (address == SLOT2_CONTROL)
    {
      _slotControl2 = value;
      RemapSlotsSEGA();
    }

    if (address >= WRAM_BASE)
    {
      WriteWRAM(address, value);
    }
    else if (_sramEnable &&
             address >= SEGA_SLOT_SIZE * 2)
    {
      var index = address & (SEGA_SLOT_SIZE - 1);
      _sram[index] = value;
    }
  }

  private void RemapSlotsSEGA()
  {
    GetBankPair(_slotControl0, out _slot0, out _slot1);
    GetBankPair(_slotControl1, out _slot2, out _slot3);

    if (!_sramEnable)
    {
      GetBankPair(_slotControl2, out _slot4, out _slot5);
      return;
    }
    
    _sram = _sramSelect
          ? _sram0
          : _sram1;
    _slot4 = _sram.Slice(0,         BANK_SIZE);
    _slot5 = _sram.Slice(BANK_SIZE, BANK_SIZE);
  }
  #endregion
}