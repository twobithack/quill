using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

using Quill.Common.Extensions;
using Quill.Core;
using Quill.Video.Definitions;

namespace Quill.Video;

public sealed partial class VDP
{
  #region Constants
  private const int VRAM_SIZE = 0x4000;
  private const int CRAM_SIZE = 0x20;
  private const int REGISTER_COUNT = 11;
  private const int HORIZONTAL_RESOLUTION = 256;
  private const int TILE_SIZE = 8;
  private const int BACKGROUND_COLUMNS = 32;
  private const int HSCROLL_LIMIT = 1;
  private const int VSCROLL_LIMIT = 24;
  private const int VCOUNTER_MAX = byte.MaxValue;
  private const byte DISABLE_SPRITES = 0xD0;
  private const byte TRANSPARENT = 0x00;
  #endregion

  #region Fields
  public bool IRQ;
  public byte HCounter;

  private readonly Framebuffer _framebuffer;
  private readonly int[] _palette;
  private readonly byte[] _vram;
  private readonly byte[] _registers;

  private ControlCode _controlCode;
  private Status _status;
  private byte _dataBuffer;

  private ushort _addressBus;
  private ushort _nameTableAddress;
  private ushort _colorTableAddress;
  private ushort _patternGeneratorTableAddress;
  private ushort _spriteAttributeTableAddress;
  private ushort _spriteGeneratorTableAddress;
  
  private ushort _vCounter;
  private byte _lineInterrupt;
  private byte _hScroll;
  private byte _vScroll;
  private byte _blankColor;

  private bool _controlWritePending;
  private bool _shiftX;
  private bool _lineInterruptEnabled;
  private bool _maskLeftBorder;
  private bool _limitHScroll;
  private bool _limitVScroll;
  private bool _zoomSprites;
  private bool _stretchSprites;
  private bool _vSyncEnabled;
  private bool _displayEnabled;
  private bool _useSecondPatternTable;

  private DisplayMode _displayMode;
  private readonly int _backgroundRows = 28;      // 32
  private readonly byte _vCounterActive = 192;    // 224
  private readonly byte _vCounterJumpFrom = 218;  // 234
  private readonly byte _vCounterJumpTo = 213;    // 229
  private bool _vCounterJumped;
  #endregion

  #region Properties
  public int ScanlinesPerFrame => VCOUNTER_MAX + (_vCounterJumpFrom - _vCounterJumpTo) + 2;
  public byte VCounter => (byte)Math.Min(_vCounter, byte.MaxValue);

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

  private bool VSyncPending => _vSyncEnabled && VBlank;
  private bool DisplayMode4 => (_displayMode & DisplayMode.Mode_4) != 0;
  #endregion

  #region Methods
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void LoadState(Snapshot snapshot)
  {
    for (byte register = 0; register < REGISTER_COUNT; register++)
      WriteRegister(register, snapshot.VRegisters[register]);
    Array.Copy(snapshot.Palette, _palette, _palette.Length);
    Array.Copy(snapshot.VRAM, _vram, _vram.Length);
    _status = snapshot.VDPStatus;
    _dataBuffer = snapshot.DataPort;
    _lineInterrupt = snapshot.LineInterrupt;
    _hScroll = snapshot.HScroll;
    _vScroll = snapshot.VScroll;
    _addressBus = (ushort)(snapshot.ControlWord & 0b_0011_1111_1111_1111);
    _controlCode = (ControlCode)(snapshot.ControlWord >> 14);
    _controlWritePending = snapshot.ControlWritePending;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void SaveState(ref Snapshot snapshot)
  {
    Array.Copy(_registers, snapshot.VRegisters, _registers.Length);
    Array.Copy(_palette, snapshot.Palette, _palette.Length);
    Array.Copy(_vram, snapshot.VRAM, _vram.Length);
    snapshot.VDPStatus = _status;
    snapshot.DataPort = _dataBuffer;
    snapshot.LineInterrupt = _lineInterrupt;
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
