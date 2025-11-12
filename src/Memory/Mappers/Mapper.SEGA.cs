using System.Runtime.CompilerServices;

using Quill.Common.Extensions;

namespace Quill.Memory;

unsafe public ref partial struct Mapper
{
  #region Constants
  private const ushort SRAM_CONTROL  = 0xFFFC;
  private const ushort SLOT0_CONTROL = 0xFFFD;
  private const ushort SLOT1_CONTROL = 0xFFFE;
  private const ushort SLOT2_CONTROL = 0xFFFF;
  #endregion
  
  #region Methods
  private void InitializeSlotsSEGA()
  {
    _slot0Control = 0x0;
    _slot1Control = 0x1;
    _slot2Control = 0x2;
  }

  public void HandleWriteSEGA(ushort address, byte value)
  {
    if (address < BANK_SIZE * 2)
      return;

    if (address == SRAM_CONTROL)
    {
      _sramEnable = value.TestBit(3);
      _sramSelect = value.TestBit(2);
      UpdateMappings();
    }
    else if (address == SLOT0_CONTROL)
    {
      _slot0Control = (byte)(value & _bankMask);
      UpdateMappings();
    }
    else if (address == SLOT1_CONTROL)
    {
      _slot1Control = (byte)(value & _bankMask);
      UpdateMappings();
    }
    else if (address == SLOT2_CONTROL)
    {
      _slot2Control = (byte)(value & _bankMask);
      UpdateMappings();
    }

    if (address < BANK_SIZE * 3)
    {
      if (!_sramEnable)
        return;
      var index = address & (BANK_SIZE - 1);
      _sram[index] = value;
    }
    else
    {
      var index = address & (RAM_SIZE - 1);
      _ram[index] = value;
    }
  }


  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void UpdateMappingsSEGA()
  {
    _sram = _sramSelect
          ? _sram0
          : _sram1;

    _slot0 = GetBank(_slot0Control);
    _slot1 = GetBank(_slot1Control);
    _slot2 = _sramEnable
           ? _sram
           : GetBank(_slot2Control);
  }
  #endregion
}