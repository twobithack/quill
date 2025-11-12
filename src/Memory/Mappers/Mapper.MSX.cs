using System.Runtime.CompilerServices;

using Quill.Memory.Definitions;

namespace Quill.Memory;

unsafe public ref partial struct Mapper
{
  #region Constants
  private const ushort SLOT0_CONTROL_MSX = 0x0002;
  private const ushort SLOT1_CONTROL_MSX = 0x0003;
  private const ushort SLOT2_CONTROL_MSX = 0x0000;
  private const ushort SLOT3_CONTROL_MSX = 0x0001;
  #endregion

  #region Methods
  private void InitializeSlotsMSX()
  {
    // TODO
  }

  private void HandleWriteMSX(ushort address, byte value)
  {
    // TODO
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void UpdateMappingsMSX()
  {
    // TODO
  }

  private static bool HasKnownMSXHash(uint crc) => Hashes.MSX.Contains(crc);
  #endregion
}