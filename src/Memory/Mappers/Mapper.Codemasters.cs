using System.Runtime.CompilerServices;

using Quill.Common.Extensions;

namespace Quill.Memory;

unsafe public ref partial struct Mapper
{
  #region Constants
  
  #endregion

  #region Methods
  private void InitializeSlotsCodemasters()
  {
    _slot0Control = 0x0;
    _slot1Control = 0x1;
    _slot2Control = 0x1;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void HandleWriteCodemasters(ushort address, byte value)
  {
    if (address < BANK_SIZE)
    {
      _slot0Control = (byte)(value & _bankMask);
      UpdateMappings();
    }
    else if (address < BANK_SIZE * 2)
    {
      _slot1Control = (byte)(value & _bankMask);
      UpdateMappings();
    }
    else if (address < BANK_SIZE * 3)
    {
      _slot2Control = (byte)(value & _bankMask);
      UpdateMappings();
    }
    else
    {
      var index = address & (RAM_SIZE - 1);
      _ram[index] = value;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void UpdateMappingsCodemasters()
  {
    _slot0 = GetBank(_slot0Control);
    _slot1 = GetBank(_slot1Control);
    _slot2 = GetBank(_slot2Control);
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