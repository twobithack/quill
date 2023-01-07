using Quill.Video.Definitions;
using System;
using static Quill.Video.VDP;

namespace Quill.Core;

[Serializable]
public class Snapshot
{
  #region Constants
  private const ushort MEMORY_SIZE = 0x4000;
  private const int CRAM_SIZE = 0x20;
  private const int REGISTER_COUNT = 11;
  #endregion

  #region Fields
  public byte[] RAM;
  public byte[] Bank0;
  public byte[] Bank1;
  public bool BankEnable;
  public bool BankSelect;
  public byte Page0;
  public byte Page1;
  public byte Page2;

  public ushort AF;
  public ushort BC;
  public ushort DE;
  public ushort HL;
  public ushort IX;
  public ushort IY;
  public ushort PC;
  public ushort SP;
  public ushort AFs;
  public ushort BCs;
  public ushort DEs;
  public ushort HLs;
  public byte I;
  public byte R;
  public bool Halted;
  public bool IFF1;
  public bool IFF2;

  public Color[] CRAM;
  public byte[] VRAM;
  public byte[] VDPRegisters;
  public Status VDPStatus;
  public byte DataPort;
  public byte LineInterrupt;
  public byte HScroll;
  public byte VScroll;
  public bool VCounterJumped;
  public bool ControlWritePending;
  #endregion

  public Snapshot()
  {
    RAM = new byte[MEMORY_SIZE];
    Bank0 = new byte[MEMORY_SIZE];
    Bank1 = new byte[MEMORY_SIZE];
    CRAM = new Color[CRAM_SIZE];
    VRAM = new byte[MEMORY_SIZE];
    VDPRegisters = new byte[REGISTER_COUNT];
  }
}
