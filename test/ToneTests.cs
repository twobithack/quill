using Quill.Core;
using Quill.CPU;
using Quill.IO;
using Quill.Memory;
using Quill.Sound;
using Quill.Video;

namespace Quill.Tests;

public class ToneTests
{
  private const int TEST_CASE_STEPS = 100000;

  [Fact]
  public void SN76489TestRomF4()
  {
    var rom = LoadROM("SN76489_TestRom_NTSC");
    var memory = new Mapper(rom);
    var psg = new PSG(new NullAudioSink());
    var vdp = new VDP(new Framebuffer());
    var bus = new Bus(memory, new(), psg, vdp);
    var cpu = new Z80(bus);

    var initialState = LoadState("SN76489_TestRom_F4");
    cpu.LoadState(initialState);

    var steps = TEST_CASE_STEPS;
    do
    {
      cpu.Step();
      steps--;
    }
    while (steps > 0);

    var state = cpu.ReadState();
    Assert.Equal(320, state.Tones[0]);
    Assert.Equal(320, state.Tones[1]);
    Assert.Equal(320, state.Tones[2]);
    Assert.Equal(1,   state.Tones[3]);

    Assert.Equal(10,  state.Volumes[0]);
    Assert.Equal(10,  state.Volumes[1]);
    Assert.Equal(10,  state.Volumes[2]);
    Assert.Equal(10,  state.Volumes[3]);
  }

  [Fact]
  public void SN76489TestRomA4()
  {
    var rom = LoadROM("SN76489_TestRom_NTSC");
    var memory = new Mapper(rom);
    var psg = new PSG(new NullAudioSink());
    var vdp = new VDP(new Framebuffer());
    var bus = new Bus(memory, new(), psg, vdp);
    var cpu = new Z80(bus);

    var initialState = LoadState("SN76489_TestRom_A4");
    cpu.LoadState(initialState);

    var steps = TEST_CASE_STEPS;
    do
    {
      cpu.Step();
      steps--;
    }
    while (steps > 0);

    var state = cpu.ReadState();
    Assert.Equal(254, state.Tones[0]);
    Assert.Equal(254, state.Tones[1]);
    Assert.Equal(254, state.Tones[2]);
    Assert.Equal(3,   state.Tones[3]);

    Assert.Equal(7,   state.Volumes[0]);
    Assert.Equal(7,   state.Volumes[1]);
    Assert.Equal(7,   state.Volumes[2]);
    Assert.Equal(7,   state.Volumes[3]);
  }

  [Fact]
  public void SN76489TestRomC5()
  {
    var rom = LoadROM("SN76489_TestRom_NTSC");
    var memory = new Mapper(rom);
    var psg = new PSG(new NullAudioSink());
    var vdp = new VDP(new Framebuffer());
    var bus = new Bus(memory, new(), psg, vdp);
    var cpu = new Z80(bus);

    var initialState = LoadState("SN76489_TestRom_C5");
    cpu.LoadState(initialState);

    var steps = TEST_CASE_STEPS;
    do
    {
      cpu.Step();
      steps--;
    }
    while (steps > 0);

    var state = cpu.ReadState();
    Assert.Equal(214, state.Tones[0]);
    Assert.Equal(214, state.Tones[1]);
    Assert.Equal(214, state.Tones[2]);
    Assert.Equal(5,   state.Tones[3]);

    Assert.Equal(4,   state.Volumes[0]);
    Assert.Equal(4,   state.Volumes[1]);
    Assert.Equal(4,   state.Volumes[2]);
    Assert.Equal(4,   state.Volumes[3]);
  }

  [Fact]
  public void SN76489TestRomE5()
  {
    var rom = LoadROM("SN76489_TestRom_NTSC");
    var memory = new Mapper(rom);
    var psg = new PSG(new NullAudioSink());
    var vdp = new VDP(new Framebuffer());
    var bus = new Bus(memory, new(), psg, vdp);
    var cpu = new Z80(bus);

    var initialState = LoadState("SN76489_TestRom_E5");
    cpu.LoadState(initialState);

    var steps = TEST_CASE_STEPS;
    do
    {
      cpu.Step();
      steps--;
    }
    while (steps > 0);

    var state = cpu.ReadState();
    Assert.Equal(170, state.Tones[0]);
    Assert.Equal(170, state.Tones[1]);
    Assert.Equal(170, state.Tones[2]);
    Assert.Equal(7,   state.Tones[3]);

    Assert.Equal(1,   state.Volumes[0]);
    Assert.Equal(1,   state.Volumes[1]);
    Assert.Equal(1,   state.Volumes[2]);
    Assert.Equal(1,   state.Volumes[3]);
  }

  private static byte[] LoadROM(string name) => File.ReadAllBytes($"roms/{name}.sms");
  private static Snapshot LoadState(string name) => Snapshot.ReadFromFile($"states/{name}.state");
}