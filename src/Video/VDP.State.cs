using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

using Quill.Common.Extensions;
using Quill.Common.Interfaces;
using Quill.Core;
using Quill.Video.Definitions;

namespace Quill.Video;

public sealed partial class VDP
{
  #region Fields
  public bool IRQ;

  private readonly IVideoSink _framebuffer;
  private readonly int[] _palette;
  private readonly byte[] _vram;
  private readonly byte[] _registers;
  private readonly bool[] _spriteMask;
  private readonly int[] _scanline;

  private ControlCode _controlCode;
  private Status _status;
  private byte _dataBuffer;

  private ushort _addressBus;
  private ushort _hCounter;
  private byte _vCounter;
  private byte _hLineCounter;
  private byte _vScroll;

  private bool _controlWriteLatch;
  private bool _hLineInterruptPending;
  private bool _vCounterJumped;
  private bool _vBlankCompleted;

  private DisplayMode _displayMode;
  #endregion

  #region Properties
  public byte HCounter => (byte)(_hCounter >> 1);
  public byte VCounter => _vCounter;
  
  private bool SpriteCollision
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => SetFlag(Status.Collision, value);
  }

  private bool SpriteOverflow
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => GetFlag(Status.Overflow);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => SetFlag(Status.Overflow, value);
  }

  private bool VBlank
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => GetFlag(Status.VBlank);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => SetFlag(Status.VBlank, value);
  }
  
  private bool SpriteShift            => TestRegisterBit(0x0, 3);
  private bool HLineInterruptEnabled  => TestRegisterBit(0x0, 4);
  private bool BlankLeftColumn        => TestRegisterBit(0x0, 5);
  private bool HScrollInhibit         => TestRegisterBit(0x0, 6);
  private bool VScrollInhibit         => TestRegisterBit(0x0, 7);

  private bool ZoomSprites            => TestRegisterBit(0x1, 0);
  private bool StretchSprites         => TestRegisterBit(0x1, 1);
  private bool VBlankInterruptEnabled => TestRegisterBit(0x1, 5);
  private bool DisplayEnabled         => TestRegisterBit(0x1, 6);

  private ushort NameTableAddress => (ushort)((_registers[0x2] & 0b_0000_1110) << 10);

  private ushort ColorTableAddress => TestRegisterBit(0x3, 7)
                                    ? (ushort)0x2000
                                    : (ushort)0x0000;

  private ushort PatternTableAddress => TestRegisterBit(0x4, 2)
                                      ? (ushort)0x2000
                                      : (ushort)0x0000;
                                               
  private ushort SpriteAttributeTableAddress => (ushort)((_registers[0x5] & 0b_0111_1110) << 7);

  private ushort SpritePatternTableAddress => TestRegisterBit(0x6, 2)
                                            ? (ushort)0x2000
                                            : (ushort)0x0000;

  private byte BlankColor => ((byte)(_registers[0x7] & 0b_1111)).SetBit(4);

  private ushort HScroll => _registers[0x8];
  
  private bool HLinePending => _hLineInterruptPending && HLineInterruptEnabled;
  private bool VSyncPending => VBlank && VBlankInterruptEnabled;
  private bool DisplayMode3 => (_displayMode & DisplayMode.Mode_3) != 0;
  private bool DisplayMode4 => (_displayMode & DisplayMode.Mode_4) != 0;
  #endregion

  #region Methods
  public void LoadState(Snapshot snapshot)
  {
    snapshot.VRegisters.AsSpan(0, _registers.Length).CopyTo(_registers);
    snapshot.Palette.AsSpan(0, _palette.Length).CopyTo(_palette);
    snapshot.VRAM.AsSpan(0, _vram.Length).CopyTo(_vram);
    _status = snapshot.VDPStatus;
    _dataBuffer = snapshot.DataPort;
    _hLineCounter = snapshot.HLineCounter;
    _hLineInterruptPending = snapshot.HLinePending;
    _vScroll = snapshot.VScroll;
    _addressBus = (ushort)(snapshot.ControlWord & 0b_0011_1111_1111_1111);
    _controlCode = (ControlCode)(snapshot.ControlWord >> 14);
    _controlWriteLatch = snapshot.ControlWriteLatch;
    IRQ = snapshot.IRQ;
  }

  public void SaveState(Snapshot snapshot)
  {
    _registers.AsSpan().CopyTo(snapshot.VRegisters);
    _palette.AsSpan().CopyTo(snapshot.Palette);
    _vram.AsSpan().CopyTo(snapshot.VRAM);
    snapshot.VDPStatus = _status;
    snapshot.DataPort = _dataBuffer;
    snapshot.HLineCounter = _hLineCounter;
    snapshot.HLinePending = _hLineInterruptPending;
    snapshot.VScroll = _vScroll;
    snapshot.ControlWord = _addressBus;
    snapshot.ControlWord |= (ushort)((byte)_controlCode << 14);
    snapshot.ControlWriteLatch = _controlWriteLatch;
    snapshot.IRQ = IRQ;
  }

  private void DumpVRAM(string path)
  {
    var memory = new List<string>();
    var row = string.Empty;

    for (ushort address = 0; address < VRAM_SIZE; address++)
    {
      if (address % 16 == 0)
      {
        memory.Add(row);
        row = $"{address.ToHex()} | ";
      }
      row += _vram[address].ToHex();
    }

    File.WriteAllLines(path, memory);
  }

  public override string ToString()
  {
    var state = $"VDP | Control: {_controlCode} | Address: {_addressBus.ToHex()} | SAT Address: {SpriteAttributeTableAddress.ToHex()}\r\n";

    for (byte register = 0; register < REGISTER_COUNT; register++)
      state += $"R{register.ToHex()}:{_registers[register].ToHex()} ";

    return state;
  }
  #endregion
}
