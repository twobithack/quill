using System.IO;

using MessagePack;
using Quill.Video.Definitions;

namespace Quill.Core;

[MessagePackObject]
public sealed class Snapshot
{
  #region Constants
  private const ushort MEMORY_SIZE = 0x4000;
  private const int PALETTE_SIZE = 0x20;
  private const int VDP_REGISTER_COUNT = 11;
  #endregion

  #region Fields
  [Key(0)]  public ushort AF;
  [Key(1)]  public ushort BC;
  [Key(2)]  public ushort DE;
  [Key(3)]  public ushort HL;
  [Key(4)]  public ushort IX;
  [Key(5)]  public ushort IY;
  [Key(6)]  public ushort PC;
  [Key(7)]  public ushort SP;
  [Key(8)]  public ushort AFs;
  [Key(9)]  public ushort BCs;
  [Key(10)] public ushort DEs;
  [Key(11)] public ushort HLs;
  [Key(12)] public byte I;
  [Key(13)] public byte R;
  [Key(14)] public bool Halt;
  [Key(15)] public bool IFF1;
  [Key(16)] public bool IFF2;

  [Key(17)] public byte[] RAM;
  [Key(18)] public byte[] Bank0;
  [Key(19)] public byte[] Bank1;
  [Key(20)] public bool BankEnable;
  [Key(21)] public bool BankSelect;
  [Key(22)] public byte Page0;
  [Key(23)] public byte Page1;
  [Key(24)] public byte Page2;

  [Key(25)] public int[] Palette;
  [Key(26)] public byte[] VRAM;
  [Key(27)] public byte[] VRegisters;
  [Key(28)] public Status VDPStatus;
  [Key(29)] public ushort ControlWord;
  [Key(30)] public byte DataPort;
  [Key(31)] public byte LineInterrupt;
  [Key(32)] public byte HScroll;
  [Key(33)] public byte VScroll;
  [Key(34)] public bool ControlWritePending;
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
  public static Snapshot ReadFromFile(string filepath)
  {
    if (!File.Exists(filepath))
      return null;

    Snapshot state;
    try
    {
      using var stream = new FileStream(filepath, FileMode.Open);
      state = MessagePackSerializer.Deserialize<Snapshot>(stream);
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
    MessagePackSerializer.Serialize(stream, this);
  }
  #endregion
}
