using Quill.Core;
using Quill.CPU;
using Quill.IO;
using Quill.Memory;
using Quill.Sound;
using Quill.Video;

namespace Quill.Tests;

public class ToneTests
{
  [Fact]
  public void SN76489_TestRom_620Hz()
  {
    var rom = LoadROM("SN76489_TestRom_NTSC");
    var memory = new Mapper(rom);
    var psg = new PSG(new NullAudioSink());
    var vdp = new VDP(new Framebuffer());
    var bus = new Bus(memory, new(), psg, vdp);
    var cpu = new Z80(bus);

    var initialState = LoadState("SN76489_TestRom_180Hz");
    cpu.LoadState(initialState);

    var steps = 1000000;
    do
    {
      cpu.Step();
      steps--;
    }
    while (steps > 0);

    var state = cpu.ReadState();
    Assert.Equal(180, state.Tones[0]);
    Assert.Equal(180, state.Tones[1]);
    Assert.Equal(180, state.Tones[2]);
    Assert.Equal(7,   state.Tones[3]);
  }

  [Fact]
  public void SN76489_TestRom_465Hz()
  {
    var rom = LoadROM("SN76489_TestRom_NTSC");
    var memory = new Mapper(rom);
    var psg = new PSG(new NullAudioSink());
    var vdp = new VDP(new Framebuffer());
    var bus = new Bus(memory, new(), psg, vdp);
    var cpu = new Z80(bus);

    var initialState = LoadState("SN76489_TestRom_240Hz");
    cpu.LoadState(initialState);

    var steps = 1000000;
    do
    {
      cpu.Step();
      steps--;
    }
    while (steps > 0);

    var state = cpu.ReadState();
    Assert.Equal(240, state.Tones[0]);
    Assert.Equal(240, state.Tones[1]);
    Assert.Equal(240, state.Tones[2]);
    Assert.Equal(5,   state.Tones[3]);
  }
  
  [Fact]
  public void SN76489_TestRom_310Hz()
  {
    var rom = LoadROM("SN76489_TestRom_NTSC");
    var memory = new Mapper(rom);
    var psg = new PSG(new NullAudioSink());
    var vdp = new VDP(new Framebuffer());
    var bus = new Bus(memory, new(), psg, vdp);
    var cpu = new Z80(bus);

    var initialState = LoadState("SN76489_TestRom_360Hz");
    cpu.LoadState(initialState);

    var steps = 1000000;
    do
    {
      cpu.Step();
      steps--;
    }
    while (steps > 0);

    var state = cpu.ReadState();
    Assert.Equal(360, state.Tones[0]);
    Assert.Equal(360, state.Tones[1]);
    Assert.Equal(360, state.Tones[2]);
    Assert.Equal(3,   state.Tones[3]);
  }

  [Fact]
  public void SN76489_TestRom_230Hz()
  {
    var rom = LoadROM("SN76489_TestRom_NTSC");
    var memory = new Mapper(rom);
    var psg = new PSG(new NullAudioSink());
    var vdp = new VDP(new Framebuffer());
    var bus = new Bus(memory, new(), psg, vdp);
    var cpu = new Z80(bus);

    var initialState = LoadState("SN76489_TestRom_480Hz");
    cpu.LoadState(initialState);

    var steps = 1000000;
    do
    {
      cpu.Step();
      steps--;
    }
    while (steps > 0);

    var state = cpu.ReadState();
    Assert.Equal(480, state.Tones[0]);
    Assert.Equal(480, state.Tones[1]);
    Assert.Equal(480, state.Tones[2]);
    Assert.Equal(1,   state.Tones[3]);
  }

  private static byte[] LoadROM(string name) => File.ReadAllBytes($"roms/{name}.sms");
  private static Snapshot LoadState(string name) => Snapshot.ReadFromFile($"states/{name}.state");
}