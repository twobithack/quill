using System;
using System.IO;

using MessagePack;
using Quill.Memory;
using Quill.Sound;
using Quill.Video;
using Quill.Video.Definitions;

namespace Quill.Core;

[MessagePackObject]
public sealed class Snapshot : IEquatable<Snapshot>
{
  #region Fields
  // CPU
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

  // Memory
  [Key(17)] public byte[] WRAM;
  [Key(18)] public byte[] SRAM0;
  [Key(19)] public byte[] SRAM1;
  [Key(20)] public byte MemoryControl;
  [Key(21)] public byte SlotControl0;
  [Key(22)] public byte SlotControl1;
  [Key(23)] public byte SlotControl2;
  [Key(24)] public byte SlotControl3;
  [Key(25)] public bool EnableSRAM;
  [Key(26)] public bool SelectSRAM;

  // VDP
  [Key(27)] public int[] Palette;
  [Key(28)] public byte[] VRAM;
  [Key(29)] public byte[] VRegisters;
  [Key(30)] public Status VDPStatus;
  [Key(31)] public ushort ControlWord;
  [Key(32)] public byte DataPort;
  [Key(33)] public byte VScroll;
  [Key(34)] public byte HLineCounter;
  [Key(35)] public bool HLinePending;
  [Key(36)] public bool ControlWriteLatch;
  [Key(37)] public bool IRQ;

  // PSG
  [Key(38)] public ushort[] Tones;
  [Key(39)] public byte[] Volumes;
  [Key(40)] public int ChannelLatch;
  [Key(41)] public bool VolumeLatch;
  #endregion

  public Snapshot()
  {
    WRAM = new byte[Mapper.WRAM_SIZE];
    SRAM0 = new byte[Mapper.SRAM_SIZE];
    SRAM1 = new byte[Mapper.SRAM_SIZE];
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
    if (AF                   != other.AF)                       return false;
    if (BC                   != other.BC)                       return false;
    if (DE                   != other.DE)                       return false;
    if (HL                   != other.HL)                       return false;
    if (IX                   != other.IX)                       return false;
    if (IY                   != other.IY)                       return false;
    if (PC                   != other.PC)                       return false;
    if (SP                   != other.SP)                       return false;
    if (AFs                  != other.AFs)                      return false;
    if (BCs                  != other.BCs)                      return false;
    if (DEs                  != other.DEs)                      return false;
    if (HLs                  != other.HLs)                      return false;
    if (I                    != other.I)                        return false;
    if (R                    != other.R)                        return false;
    if (Halt                 != other.Halt)                     return false;
    if (IFF1                 != other.IFF1)                     return false;
    if (IFF2                 != other.IFF2)                     return false;
    if (EnableSRAM           != other.EnableSRAM)               return false;
    if (SelectSRAM           != other.SelectSRAM)               return false;
    if (MemoryControl        != other.MemoryControl)            return false;
    if (SlotControl0         != other.SlotControl0)             return false;
    if (SlotControl1         != other.SlotControl1)             return false;
    if (SlotControl2         != other.SlotControl2)             return false;
    if (SlotControl3         != other.SlotControl3)             return false;
    if (VDPStatus            != other.VDPStatus)                return false;
    if (ControlWord          != other.ControlWord)              return false;
    if (DataPort             != other.DataPort)                 return false;
    if (VScroll              != other.VScroll)                  return false;
    if (HLineCounter         != other.HLineCounter)             return false;
    if (HLinePending         != other.HLinePending)             return false;
    if (ControlWriteLatch   != other.ControlWriteLatch)      return false;
    if (IRQ                  != other.IRQ)                      return false;
    if (ChannelLatch         != other.ChannelLatch)             return false;
    if (VolumeLatch          != other.VolumeLatch)              return false;
    if (!WRAM.AsSpan()       .SequenceEqual(other.WRAM))        return false;
    if (!SRAM0.AsSpan()      .SequenceEqual(other.SRAM0))       return false;
    if (!SRAM1.AsSpan()      .SequenceEqual(other.SRAM1))       return false;
    if (!Palette.AsSpan()    .SequenceEqual(other.Palette))     return false;
    if (!VRAM.AsSpan()       .SequenceEqual(other.VRAM))        return false;
    if (!VRegisters.AsSpan() .SequenceEqual(other.VRegisters))  return false;
    if (!Tones.AsSpan()      .SequenceEqual(other.Tones))       return false;
    if (!Volumes.AsSpan()    .SequenceEqual(other.Volumes))     return false;

    return true;
  }

  public override bool Equals(object obj) => obj is Snapshot other
                                          && Equals(other);

  public override int GetHashCode() => HashCode.Combine(AF, BC, DE, HL,
                                                        IX, IY, PC, SP);
  #endregion
}
