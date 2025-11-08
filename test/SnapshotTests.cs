using Quill.Core;
using Quill.CPU;
using Quill.IO;
using Quill.Memory;
using Quill.Sound;
using Quill.Video;

namespace Quill.Tests;

public class SnapshotTests
{
  #region Constants
  private const int TEST_CASE_STEP_LIMIT = 9000000;
  private const int LONG_TEST_CASE_STEP_LIMIT = 468281085;
  private const ushort SMSMEMTEST_RAM_TEST_HOOK  = 0x0524;
  private const ushort SMSMEMTEST_VRAM_TEST_HOOK = 0x0618;
  private const ushort SMSMEMTEST_SRAM_TEST_HOOK = 0x0724;
  private const ushort ZEXDOC_TEST_CASE_HOOK = 0x2C1E;
  #endregion

  [Fact]
  public void SMSmemtestRAM()
  {
    var rom = LoadROM("SMSmemtest");
    var memory = new Mapper(rom);
    var psg = new PSG(new NullAudioSink());
    var vdp = new VDP(new Framebuffer());
    var bus = new Bus(memory, new(), psg, vdp);
    var cpu = new Z80(bus);

    var steps = TEST_CASE_STEP_LIMIT;
    do
    {
      cpu.Step();
      steps--;

      Assert.True(steps > 0, "CPU step limit exceeded.");
    }
    while (cpu.PC != SMSMEMTEST_RAM_TEST_HOOK);

    var state = cpu.ReadState();
    var targetState = LoadState("SMSmemtest_RAM");
    Assert.Equal(state, targetState);

    var frame = vdp.ReadFramebuffer();
    var targetFrame = LoadFrame("SMSmemtest_RAM");
    Assert.Equal(frame, targetFrame);
  }

  [Fact]
  public void SMSmemtestVRAM()
  {
    var rom = LoadROM("SMSmemtest");
    var memory = new Mapper(rom);
    var psg = new PSG(new NullAudioSink());
    var vdp = new VDP(new Framebuffer());
    var bus = new Bus(memory, new(), psg, vdp);
    var cpu = new Z80(bus);

    var initialState = LoadState("SMSmemtest_VRAM_init");
    cpu.LoadState(initialState);

    var steps = TEST_CASE_STEP_LIMIT;
    do
    {
      cpu.Step();
      steps--;

      Assert.True(steps > 0, "CPU step limit exceeded.");
    }
    while (cpu.PC != SMSMEMTEST_VRAM_TEST_HOOK);

    var state = cpu.ReadState();
    var targetState = LoadState("SMSmemtest_VRAM");
    Assert.Equal(state, targetState);

    var frame = vdp.ReadFramebuffer();
    var targetFrame = LoadFrame("SMSmemtest_VRAM");
    Assert.Equal(frame, targetFrame);
  }

  [Fact]
  public void SMSmemtestSRAM()
  {
    var rom = LoadROM("SMSmemtest");
    var memory = new Mapper(rom);
    var psg = new PSG(new NullAudioSink());
    var vdp = new VDP(new Framebuffer());
    var bus = new Bus(memory, new(), psg, vdp);
    var cpu = new Z80(bus);

    var initialState = LoadState("SMSmemtest_SRAM_init");
    cpu.LoadState(initialState);

    var steps = TEST_CASE_STEP_LIMIT;
    do
    {
      cpu.Step();
      steps--;

      Assert.True(steps > 0, "CPU step limit exceeded.");
    }
    while (cpu.PC != SMSMEMTEST_SRAM_TEST_HOOK);

    var state = cpu.ReadState();
    var targetState = LoadState("SMSmemtest_SRAM");
    Assert.Equal(state, targetState);
    
    var frame = vdp.ReadFramebuffer();
    var targetFrame = LoadFrame("SMSmemtest_SRAM");
    Assert.Equal(frame, targetFrame);
  }

  [Fact]
  public void Zexdoc()
  {
    var rom = LoadROM("zexdoc");
    var memory = new Mapper(rom);
    var psg = new PSG(new NullAudioSink());
    var vdp = new VDP(new Framebuffer());
    var bus = new Bus(memory, new(), psg, vdp);
    var cpu = new Z80(bus);

    for (int testCase = 0; testCase < 50; testCase++)
    {
      var steps = TEST_CASE_STEP_LIMIT;
      do
      {
        cpu.Step();
        steps--;

        Assert.True(steps > 0, "CPU step limit exceeded.");
      }
      while (cpu.PC != ZEXDOC_TEST_CASE_HOOK);

      var state = cpu.ReadState();
      var targetState = LoadState($"zexdoc_{testCase:D2}");
      Assert.Equal(state, targetState);

      var frame = vdp.ReadFramebuffer();
      var targetFrame = LoadFrame($"zexdoc_{testCase:D2}");
      Assert.Equal(frame, targetFrame);
    }
  }

  [Fact, Trait("Category", "Long")]
  public void ZexdocFull()
  {
    var rom = LoadROM("zexdoc");
    var memory = new Mapper(rom);
    var psg = new PSG(new NullAudioSink());
    var vdp = new VDP(new Framebuffer());
    var bus = new Bus(memory, new(), psg, vdp);
    var cpu = new Z80(bus);

    for (int testCase = 0; testCase < 79; testCase++)
    {
      var steps = LONG_TEST_CASE_STEP_LIMIT;
      do
      {
        cpu.Step();
        steps--;

        Assert.True(steps > 0, "CPU step limit exceeded.");
      }
      while (cpu.PC != ZEXDOC_TEST_CASE_HOOK);

      var state = cpu.ReadState();
      var targetState = LoadState($"zexdoc_{testCase:D2}");
      Assert.Equal(state, targetState);

      var frame = vdp.ReadFramebuffer();
      var targetFrame = LoadFrame($"zexdoc_{testCase:D2}");
      Assert.Equal(frame, targetFrame);
    }
  }

  private static byte[] LoadROM(string name) => File.ReadAllBytes($"roms/{name}.sms");
  private static Snapshot LoadState(string name) => Snapshot.ReadFromFile($"states/{name}.state");
  private static byte[] LoadFrame(string name) => File.ReadAllBytes($"frames/{name}.frame");
}