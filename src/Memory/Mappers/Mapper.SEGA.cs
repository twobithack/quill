using System.Runtime.CompilerServices;

using Quill.Common.Extensions;

namespace Quill.Memory;

unsafe public ref partial struct Mapper
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
    _slot0Control = 0x0;
    _slot1Control = 0x1;
    _slot2Control = 0x2;
  }

  public void WriteByteSEGA(ushort address, byte value)
  {
    if (address < SEGA_SLOT_SIZE * 2)
      return;

    if (address == SRAM_CONTROL)
    {
      _sramEnable = value.TestBit(3);
      _sramSelect = value.TestBit(2);
      RemapSlotsSEGA();
    }
    else if (address == SLOT0_CONTROL)
    {
      _slot0Control = value;
      RemapSlotsSEGA();
    }
    else if (address == SLOT1_CONTROL)
    {
      _slot1Control = value;
      RemapSlotsSEGA();
    }
    else if (address == SLOT2_CONTROL)
    {
      _slot2Control = value;
      RemapSlotsSEGA();
    }

    if (address < SEGA_SLOT_SIZE * 3)
    {
      if (!_sramEnable)
        return;
      var index = address & (BANK_SIZE - 1);
      _sram[index] = value;
    }
    else
    {
      var index = address & (BANK_SIZE - 1);
      _ram[index] = value;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RemapSlotsSEGA()
  {
    _sram = _sramSelect
          ? _sram0
          : _sram1;

    var bank0 = (byte)((_slot0Control << 1) & _bankMask);
    _slot0 = GetBank(bank0);
    _slot1 = GetBank(bank0.Increment());

    var bank1 = (byte)((_slot1Control << 1) & _bankMask);
    _slot2 = GetBank(bank1);
    _slot3 = GetBank(bank1.Increment());

    if (_sramEnable)
    {
      _slot4 = _sram.Slice(0,         BANK_SIZE);
      _slot5 = _sram.Slice(BANK_SIZE, BANK_SIZE);
    }
    else
    {
      var bank2 = (byte)((_slot2Control << 1) & _bankMask);
      _slot4 = GetBank(bank2);
      _slot5 = GetBank(bank2.Increment());
    }
  }
  #endregion
}