using System;
using System.IO.Hashing;
using System.Runtime.CompilerServices;

using Quill.Common.Extensions;
using Quill.Memory.Definitions;

namespace Quill.Memory;

unsafe public ref partial struct Mapper
{
  #region Constants
  private const ushort SLOT2_CONTROL_KOREAN = 0xA000;
  #endregion

  #region Methods
  private void InitializeSlotsKorean()
  {
    _slot0Control = 0x0;
    _slot1Control = 0x1;
    _slot2Control = 0x1;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void HandleWriteKorean(ushort address, byte value)
  {
    if (address == SLOT2_CONTROL_KOREAN)
    {
      _slot2Control = (byte)(value & _bankMask);
      UpdateMappings();
    }
    else if (address >= BANK_SIZE * 3)
    {
      var index = address & (RAM_SIZE - 1);
      _ram[index] = value;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void UpdateMappingsKorean()
  {
    _slot0 = GetBank(0);
    _slot1 = GetBank(1);
    _slot2 = GetBank(_slot2Control);
  }

  private static bool HasKnownKoreanHash(uint crc) => Hashes.Korean.Contains(crc);
  #endregion
}