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

  private ControlCode _controlCode;
  private Status _status;
  private byte _dataBuffer;

  private ushort _addressBus;
  private ushort _nameTableAddress;
  private ushort _colorTableAddress;
  private ushort _patternGeneratorTableAddress;
  private ushort _spriteAttributeTableAddress;
  private ushort _spriteGeneratorTableAddress;
  
  private ushort _hCounter;
  private byte _vCounter;
  private byte _hLineCounter;
  private byte _hScroll;
  private byte _vScroll;
  private byte _blankColor;
  private byte _legacyBlankColor;

  private bool _controlWritePending;
  private bool _hLineInterruptPending;
  private bool _hLineInterruptEnabled;
  private bool _vBlankInterruptEnabled;
  private bool _displayEnabled;
  private bool _spriteShift;
  private bool _leftColumnBlank;
  private bool _hScrollInhibit;
  private bool _vScrollInhibit;
  private bool _zoomSprites;
  private bool _stretchSprites;
  private bool _useSecondPatternTable;

  private DisplayMode _displayMode;
  private readonly int _backgroundRows = 28;
  private readonly byte _vCounterActive = 191;
  private readonly byte _vCounterJumpFrom = 218;
  private readonly byte _vCounterJumpTo = 213;
  private bool _vCounterJumped;
  private bool _vBlankCompleted;
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

  private bool HLinePending => _hLineInterruptPending && _hLineInterruptEnabled;
  private bool VSyncPending => VBlank && _vBlankInterruptEnabled;
  private bool DisplayMode4 => (_displayMode & DisplayMode.Mode_4) != 0;
  #endregion

  #region Methods
  public void LoadState(Snapshot snapshot)
  {
    for (byte register = 0; register < REGISTER_COUNT; register++)
      WriteRegister(register, snapshot.VRegisters[register]);
    snapshot.Palette.AsSpan(0, _palette.Length).CopyTo(_palette);
    snapshot.VRAM.AsSpan(0, _vram.Length).CopyTo(_vram);
    _status = snapshot.VDPStatus;
    _dataBuffer = snapshot.DataPort;
    _hLineCounter = snapshot.HLineCounter;
    _hLineInterruptPending = snapshot.HLinePending;
    _hScroll = snapshot.HScroll;
    _vScroll = snapshot.VScroll;
    _addressBus = (ushort)(snapshot.ControlWord & 0b_0011_1111_1111_1111);
    _controlCode = (ControlCode)(snapshot.ControlWord >> 14);
    _controlWritePending = snapshot.ControlWritePending;
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
    snapshot.HScroll = _hScroll;
    snapshot.VScroll = _vScroll;
    snapshot.ControlWord = _addressBus;
    snapshot.ControlWord |= (ushort)((byte)_controlCode << 14);
    snapshot.ControlWritePending = _controlWritePending;
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
    var state = $"VDP | Control: {_controlCode} | Address: {_addressBus.ToHex()} | SAT Address: {_spriteAttributeTableAddress.ToHex()}\r\n";

    for (byte register = 0; register < REGISTER_COUNT; register++)
      state += $"R{register.ToHex()}:{_registers[register].ToHex()} ";

    return state;
  }
  #endregion
}
