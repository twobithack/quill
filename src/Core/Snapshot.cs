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
  [Key(18)] public byte[] SRAM0;
  [Key(19)] public byte[] SRAM1;
  [Key(20)] public bool EnableSRAM;
  [Key(21)] public bool SelectSRAM;
  [Key(22)] public byte Slot0Control;
  [Key(23)] public byte Slot1Control;
  [Key(24)] public byte Slot2Control;

  [Key(25)] public int[] Palette;
  [Key(26)] public byte[] VRAM;
  [Key(27)] public byte[] VRegisters;
  [Key(28)] public Status VDPStatus;
  [Key(29)] public ushort ControlWord;
  [Key(30)] public byte DataPort;
  [Key(31)] public byte VScroll;
  [Key(32)] public byte HLineCounter;
  [Key(33)] public bool HLinePending;
  [Key(34)] public bool ControlWritePending;
  [Key(35)] public bool IRQ;

  [Key(36)] public ushort[] Tones;
  [Key(37)] public byte[] Volumes;
  [Key(38)] public int ChannelLatch;
  [Key(39)] public bool VolumeLatch;
  #endregion

  public Snapshot()
  {
    RAM = new byte[Mapper.BANK_SIZE*2];
    SRAM0 = new byte[Mapper.BANK_SIZE*2];
    SRAM1 = new byte[Mapper.BANK_SIZE*2];
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
    if (AF                  != other.AF)                      return false;
    if (BC                  != other.BC)                      return false;
    if (DE                  != other.DE)                      return false;
    if (HL                  != other.HL)                      return false;
    if (IX                  != other.IX)                      return false;
    if (IY                  != other.IY)                      return false;
    if (PC                  != other.PC)                      return false;
    if (SP                  != other.SP)                      return false;
    if (AFs                 != other.AFs)                     return false;
    if (BCs                 != other.BCs)                     return false;
    if (DEs                 != other.DEs)                     return false;
    if (HLs                 != other.HLs)                     return false;
    if (I                   != other.I)                       return false;
    if (R                   != other.R)                       return false;
    if (Halt                != other.Halt)                    return false;
    if (IFF1                != other.IFF1)                    return false;
    if (IFF2                != other.IFF2)                    return false;
    if (EnableSRAM          != other.EnableSRAM)              return false;
    if (SelectSRAM          != other.SelectSRAM)              return false;
    if (Slot0Control        != other.Slot0Control)            return false;
    if (Slot1Control        != other.Slot1Control)            return false;
    if (Slot2Control        != other.Slot2Control)            return false;
    if (VDPStatus           != other.VDPStatus)               return false;
    if (ControlWord         != other.ControlWord)             return false;
    if (DataPort            != other.DataPort)                return false;
    if (VScroll             != other.VScroll)                 return false;
    if (HLineCounter        != other.HLineCounter)            return false;
    if (HLinePending        != other.HLinePending)            return false;
    if (ControlWritePending != other.ControlWritePending)     return false;
    if (IRQ                 != other.IRQ)                     return false;
    if (ChannelLatch        != other.ChannelLatch)            return false;
    if (VolumeLatch         != other.VolumeLatch)             return false;
    if (!RAM.AsSpan()       .SequenceEqual(other.RAM))        return false;
    if (!SRAM0.AsSpan()     .SequenceEqual(other.SRAM0))      return false;
    if (!SRAM1.AsSpan()     .SequenceEqual(other.SRAM1))      return false;
    if (!Palette.AsSpan()   .SequenceEqual(other.Palette))    return false;
    if (!VRAM.AsSpan()      .SequenceEqual(other.VRAM))       return false;
    if (!VRegisters.AsSpan().SequenceEqual(other.VRegisters)) return false;
    if (!Tones.AsSpan()     .SequenceEqual(other.Tones))      return false;
    if (!Volumes.AsSpan()   .SequenceEqual(other.Volumes))    return false;
    
    return true;
  }

  public override bool Equals(object obj) => obj is Snapshot other
                                          && Equals(other);

  public override int GetHashCode() => HashCode.Combine(AF, BC, DE, HL,
                                                        IX, IY, PC, SP);
  #endregion
}
