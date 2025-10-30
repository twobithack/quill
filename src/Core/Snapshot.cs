using static System.Collections.StructuralComparisons;
using System.IO;

using MessagePack;
using Quill.Memory;
using Quill.Sound;
using Quill.Video;
using Quill.Video.Definitions;

namespace Quill.Core;

[MessagePackObject]
public sealed class Snapshot
{
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
  [Key(31)] public byte HScroll;
  [Key(32)] public byte VScroll;
  [Key(33)] public byte HLineCounter;
  [Key(34)] public bool HLinePending;
  [Key(35)] public bool ControlWritePending;

  [Key(36)] public ushort[] Tones;
  [Key(37)] public byte[] Volumes;
  [Key(38)] public int ChannelLatch;
  [Key(39)] public bool VolumeLatch;
  #endregion

  public Snapshot()
  {
    RAM = new byte[Mapper.PAGE_SIZE];
    Bank0 = new byte[Mapper.PAGE_SIZE];
    Bank1 = new byte[Mapper.PAGE_SIZE];
    Palette = new int[VDP.CRAM_SIZE];
    VRAM = new byte[VDP.VRAM_SIZE];
    VRegisters = new byte[VDP.REGISTER_COUNT];
    Tones = new ushort[PSG.CHANNEL_COUNT];
    Volumes = new byte[PSG.CHANNEL_COUNT];
  }
  
  #region Methods
  public static Snapshot ReadFromFile(string filepath)
  {
    if (!File.Exists(filepath))
      return null;

    try
    {
      using var stream = new FileStream(filepath, FileMode.Open);
      return MessagePackSerializer.Deserialize<Snapshot>(stream);
    }
    catch
    {
      return null;
    }
  }

  public void WriteToFile(string filepath)
  {
    using var stream = new FileStream(filepath, FileMode.Create);
    MessagePackSerializer.Serialize(stream, this);
  }

  public bool Equals(Snapshot other)
  {
    if (!StructuralEqualityComparer.Equals(RAM,        other.RAM))        return false;
    if (!StructuralEqualityComparer.Equals(Bank0,      other.Bank0))      return false;
    if (!StructuralEqualityComparer.Equals(Bank1,      other.Bank1))      return false;
    if (!StructuralEqualityComparer.Equals(Palette,    other.Palette))    return false;
    if (!StructuralEqualityComparer.Equals(VRAM,       other.VRAM))       return false;
    if (!StructuralEqualityComparer.Equals(VRegisters, other.VRegisters)) return false;

    if (AF   != other.AF)   return false;
    if (BC   != other.BC)   return false;
    if (DE   != other.DE)   return false;
    if (HL   != other.HL)   return false;
    if (IX   != other.IX)   return false;
    if (IY   != other.IY)   return false;
    if (PC   != other.PC)   return false;
    if (SP   != other.SP)   return false;
    if (AFs  != other.AFs)  return false;
    if (BCs  != other.BCs)  return false;
    if (DEs  != other.DEs)  return false;
    if (HLs  != other.HLs)  return false;
    if (I    != other.I)    return false;
    if (R    != other.R)    return false;
    if (IFF1 != other.IFF1) return false;
    if (IFF2 != other.IFF2) return false;

    return true;
  }
  #endregion
}
