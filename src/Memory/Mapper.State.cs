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
  private readonly Span<byte> _sram0;
  private readonly Span<byte> _sram1;
  private readonly Span<byte> _wram;

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

  private byte _memoryControl;
  private byte _slotControl0;
  private byte _slotControl1;
  private byte _slotControl2;
  private byte _slotControl3;
  private bool _sramEnable;
  private bool _sramSelect;

  private ReadOnlySpan<byte> _romReversed;
  #endregion

  #region Properties
  private readonly bool EnableBIOS      => !_memoryControl.TestBit(3);
  private readonly bool EnableWRAM      => !_memoryControl.TestBit(4);
  private readonly bool EnableCard      => !_memoryControl.TestBit(5);
  private readonly bool EnableCartridge => !_memoryControl.TestBit(6);
  private readonly bool EnableExpansion => !_memoryControl.TestBit(7);
  #endregion

  #region Methods
  public void LoadState(Snapshot state)
  {
    state.WRAM.AsSpan(0, BANK_SIZE).CopyTo(_wram);
    state.SRAM0.AsSpan().CopyTo(_sram0);
    state.SRAM1.AsSpan().CopyTo(_sram1);
    _slotControl0 = state.SlotControl0;
    _slotControl1 = state.SlotControl1;
    _slotControl2 = state.SlotControl2;
    _sramEnable   = state.EnableSRAM;
    _sramSelect   = state.SelectSRAM;
    RemapSlots();
  }

  public readonly void SaveState(Snapshot state)
  {
    _wram.CopyTo(state.WRAM);
    _sram0.CopyTo(state.SRAM0);
    _sram1.CopyTo(state.SRAM1);
    state.SlotControl0 = _slotControl0;
    state.SlotControl1 = _slotControl1;
    state.SlotControl2 = _slotControl2;
    state.EnableSRAM   = _sramEnable;
    state.SelectSRAM   = _sramSelect;
  }

  public readonly void DumpWRAM(string path)
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
      row += _wram[address].ToHex();
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
    return $"Memory: RAM banking {banking} | P0: {_slotControl0.ToHex()}, P1: {_slotControl1.ToHex()}, P2: {_slotControl2.ToHex()}";
  }
  #endregion
}