using Quill.Common.Extensions;
using Quill.Core;
using Quill.Video.Definitions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

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
  private ushort _vCounter;
  private ushort _controlWord;
  private Status _status;
  private byte _dataBuffer;
  private byte _lineInterrupt;
  private byte _hScroll;
  private byte _vScroll;
  private bool _controlWritePending;
  private bool _vCounterJumped;
  private bool _frameQueued;

  // TODO: derive from display mode
  private readonly int _backgroundRows = 28;
  private readonly int _vCounterMax = 255;
  private readonly byte _vCounterActive = 192;
  private readonly byte _vCounterJumpStart = 0xDA;
  private readonly byte _vCounterJumpEnd = 0xD5;
  #endregion

  #region Properties
  public int ScanlinesPerFrame => _vCounterMax + (_vCounterJumpStart - _vCounterJumpEnd);
  public byte VCounter => (byte)Math.Min(_vCounter, byte.MaxValue);

  private ControlCode ControlCode => (ControlCode)(_controlWord >> 14);
  private ushort Address => (ushort)(_controlWord & 0b_0011_1111_1111_1111);
  private bool ShiftX => TestRegisterBit(0x0, 3);
  private bool LineInterruptEnabled => TestRegisterBit(0x0, 4);
  private bool MaskLeftBorder => TestRegisterBit(0x0, 5);
  private bool LimitHScroll => TestRegisterBit(0x0, 6);
  private bool LimitVScroll => TestRegisterBit(0x0, 7);
  private bool ZoomSprites => TestRegisterBit(0x1, 0);
  private bool StretchSprites => TestRegisterBit(0x1, 1);
  private bool VSyncEnabled => TestRegisterBit(0x1, 5);
  private bool DisplayEnabled => TestRegisterBit(0x1, 6);
  private bool UseSecondPatternTable => TestRegisterBit(0x6, 2);
  private byte BackgroundColor => (byte)(_registers[0x7] & 0b_0011);

  private bool SpriteCollision
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => SetFlag(Status.Collision, value);
  }

  private bool SpriteOverflow
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => _status.HasFlag(Status.Overflow);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => SetFlag(Status.Overflow, value);
  }

  private bool VBlank
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => _status.HasFlag(Status.VBlank);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set => SetFlag(Status.VBlank, value);
  }

  private bool VSyncPending => VSyncEnabled && VBlank;
  #endregion

  #region Methods
  public void LoadState(Snapshot snapshot)
  {
    Array.Copy(snapshot.Palette, _palette, _palette.Length);
    Array.Copy(snapshot.VRAM, _vram, _vram.Length);
    Array.Copy(snapshot.VRegisters, _registers, _registers.Length);
    _status = snapshot.VDPStatus;
    _dataBuffer = snapshot.DataPort;
    _lineInterrupt = snapshot.LineInterrupt;
    _hScroll = snapshot.HScroll;
    _vScroll = snapshot.VScroll;
    _controlWord = snapshot.ControlWord;
    _controlWritePending = snapshot.ControlWritePending;
  }

  public void SaveState(ref Snapshot snapshot)
  {
    Array.Copy(_palette, snapshot.Palette, _palette.Length);
    Array.Copy(_vram, snapshot.VRAM, _vram.Length);
    Array.Copy(_registers, snapshot.VRegisters, _registers.Length);
    snapshot.VDPStatus = _status;
    snapshot.DataPort = _dataBuffer;
    snapshot.LineInterrupt = _lineInterrupt;
    snapshot.HScroll = _hScroll;
    snapshot.VScroll = _vScroll;
    snapshot.ControlWord = _controlWord;
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
    var state = $"VDP | Control: {ControlCode} | Address: {Address.ToHex()} | SAT Address: {GetSpriteAttributeTableAddress().ToHex()}\r\n";

    for (byte register = 0; register < REGISTER_COUNT; register++)
      state += $"R{register.ToHex()}:{_registers[register].ToHex()} ";

    return state;
  }
  #endregion
}
