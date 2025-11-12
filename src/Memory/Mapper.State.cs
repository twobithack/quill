using System;
using System.Collections.Generic;
using System.IO;

using Quill.Common.Extensions;
using Quill.Core;
using Quill.Memory.Definitions;

namespace Quill.Memory;

unsafe public ref partial struct Mapper
{
  #region Fields
  private readonly ReadOnlySpan<byte> _rom;
  private readonly Span<byte> _ram;
  private readonly Span<byte> _sram0;
  private readonly Span<byte> _sram1;

  private ReadOnlySpan<byte> _vectors;
  private ReadOnlySpan<byte> _slot0;
  private ReadOnlySpan<byte> _slot1;
  private ReadOnlySpan<byte> _slot2;
  private ReadOnlySpan<byte> _slot3;
  private ReadOnlySpan<byte> _slot4;
  private ReadOnlySpan<byte> _slot5;
  private Span<byte> _sram;

  private readonly MapperType _mapper;
  private readonly int _bankCount;
  private readonly byte _bankMask;
  private byte _slot0Control;
  private byte _slot1Control;
  private byte _slot2Control;
  private byte _slot3Control;
  private bool _sramEnable;
  private bool _sramSelect;
  #endregion

  #region Methods
  public void LoadState(Snapshot state)
  {
    state.RAM.AsSpan().CopyTo(_ram);
    state.SRAM0.AsSpan().CopyTo(_sram0);
    state.SRAM1.AsSpan().CopyTo(_sram1);
    _slot0Control = state.Slot0Control;
    _slot1Control = state.Slot1Control;
    _slot2Control = state.Slot2Control;
    _sramEnable   = state.EnableSRAM;
    _sramSelect   = state.SelectSRAM;
    UpdateSlots();
  }

  public readonly void SaveState(Snapshot state)
  {
    _ram.CopyTo(state.RAM);
    _sram0.CopyTo(state.SRAM0);
    _sram1.CopyTo(state.SRAM1);
    state.Slot0Control = _slot0Control;
    state.Slot1Control = _slot1Control;
    state.Slot2Control = _slot2Control;
    state.EnableSRAM   = _sramEnable;
    state.SelectSRAM   = _sramSelect;
  }

  public readonly void DumpRAM(string path)
  {
    var memory = new List<string>();
    var row = string.Empty;

    for (ushort address = 0; address < BANK_SIZE / 2; address++)
    {
      if (address % 64 == 0)
      {
        memory.Add(row);
        row = string.Empty;
      }
      row += _ram[address].ToHex();
    }

    File.WriteAllLines(path, memory);
  }
  
  public readonly void DumpROM(string path)
  {
    var dump = new List<string>();
    for (byte page = 0; page < 0x40; page++)
    {
      var row = $"PAGE {page.ToHex()}";
      var rowBytes = _rom.Slice(page * BANK_SIZE, BANK_SIZE);
      for (ushort index = 0; index < BANK_SIZE; index++)
      {
        if (index % 16 == 0)
        {
          dump.Add(row);
          row = $"{index.ToHex()} : ";
        }
        row += rowBytes[index].ToHex();
      }
    }
    File.WriteAllLines(path, dump);
  }

  public override readonly string ToString()
  {
    var banking = _sramEnable ? $"enabled (Bank {_sramSelect.ToBit()})" : "disabled";
    return $"Memory: RAM banking {banking} | P0: {_slot0Control.ToHex()}, P1: {_slot1Control.ToHex()}, P2: {_slot2Control.ToHex()}";
  }
  #endregion
}