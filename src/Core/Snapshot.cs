using Quill.Video;
using Quill.Video.Definitions;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Quill.Core;

[Serializable]
public sealed class Snapshot
{
  #region Constants
  private const ushort MEMORY_SIZE = 0x4000;
  private const int PALETTE_SIZE = 0x20;
  private const int VDP_REGISTER_COUNT = 11;
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
  public bool Halt;
  public bool IFF1;
  public bool IFF2;

  public int[] Palette;
  public byte[] VRAM;
  public byte[] VRegisters;
  public Status VDPStatus;
  public ushort ControlWord;
  public byte DataPort;
  public byte LineInterrupt;
  public byte HScroll;
  public byte VScroll;
  public bool ControlWritePending;
  #endregion

  public Snapshot()
  {
    RAM = new byte[MEMORY_SIZE];
    Bank0 = new byte[MEMORY_SIZE];
    Bank1 = new byte[MEMORY_SIZE];
    Palette = new int[PALETTE_SIZE];
    VRAM = new byte[MEMORY_SIZE];
    VRegisters = new byte[VDP_REGISTER_COUNT];
  }
  
  #region Methods
  #pragma warning disable SYSLIB0011
  public static Snapshot ReadFromFile(string filepath)
  {
    if (!File.Exists(filepath))
      return null;

    Snapshot state;
    try
    {
      using var stream = new FileStream(filepath, FileMode.Open);
      var formatter = new BinaryFormatter();
      state = (Snapshot)formatter.Deserialize(stream);
    }
    catch
    {
      return null;
    }

    return state;
  }

  public void WriteToFile(string filepath)
  {
    using var stream = new FileStream(filepath, FileMode.Create);
    var formatter = new BinaryFormatter();
    formatter.Serialize(stream, this);
  }
  #pragma warning restore SYSLIB0011
  #endregion
}
