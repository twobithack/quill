using static System.Collections.StructuralComparisons;

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
  private const int TEST_CASE_STEP_LIMIT = 468281085;
  private const ushort SMSMEMTEST_RAM_TEST_HOOK  = 0x0524;
  private const ushort SMSMEMTEST_VRAM_TEST_HOOK = 0x0618;
  private const ushort SMSMEMTEST_SRAM_TEST_HOOK = 0x0724;
  private const ushort ZEXDOC_TEST_CASE_HOOK = 0x2C1E;
  #endregion

  [Fact]
  public void SMSmemtest_RAM()
  {
    var rom = LoadROM("SMSmemtest");
    var memory = new Mapper(rom);
    var psg = new PSG(new NullAudioSink());
    var vdp = new VDP(new Framebuffer());
    var bus = new Bus(memory, new(), psg, vdp);
    var cpu = new Z80(bus);

    var steps = 0;
    do
    {
      cpu.Step();
      steps++;

      Assert.False(steps > TEST_CASE_STEP_LIMIT, "CPU step limit exceeded.");
    }
    while (cpu.PC != SMSMEMTEST_RAM_TEST_HOOK);

    var state = cpu.ReadState();
    var targetState = LoadState("SMSmemtest_RAM");
    Assert.True(state.Equals(targetState), "Snapshot mismatch.");

    var frame = vdp.ReadFramebuffer();
    var targetFrame = LoadFrame("SMSmemtest_RAM");
    Assert.True(CompareFrames(frame, targetFrame), "Framebuffer mismatch.");
  }

  [Fact]
  public void SMSmemtest_VRAM()
  {
    var rom = LoadROM("SMSmemtest");
    var memory = new Mapper(rom);
    var psg = new PSG(new NullAudioSink());
    var vdp = new VDP(new Framebuffer());
    var bus = new Bus(memory, new(), psg, vdp);
    var cpu = new Z80(bus);

    var initialState = LoadState("SMSmemtest_VRAM_init");
    cpu.LoadState(initialState);

    var steps = 0;
    do
    {
      cpu.Step();
      steps++;

      Assert.False(steps > TEST_CASE_STEP_LIMIT, "CPU step limit exceeded.");
    }
    while (cpu.PC != SMSMEMTEST_VRAM_TEST_HOOK);

    var state = cpu.ReadState();
    var targetState = LoadState("SMSmemtest_VRAM");
    Assert.True(state.Equals(targetState), "Snapshot mismatch.");

    var frame = vdp.ReadFramebuffer();
    var targetFrame = LoadFrame("SMSmemtest_VRAM");
    Assert.True(CompareFrames(frame, targetFrame), "Framebuffer mismatch.");
  }

  [Fact]
  public void SMSmemtest_SRAM()
  {
    var rom = LoadROM("SMSmemtest");
    var memory = new Mapper(rom);
    var psg = new PSG(new NullAudioSink());
    var vdp = new VDP(new Framebuffer());
    var bus = new Bus(memory, new(), psg, vdp);
    var cpu = new Z80(bus);

    var initialState = LoadState("SMSmemtest_SRAM_init");
    cpu.LoadState(initialState);

    var steps = 0;
    do
    {
      cpu.Step();
      steps++;

      Assert.False(steps > TEST_CASE_STEP_LIMIT, "CPU step limit exceeded.");
    }
    while (cpu.PC != SMSMEMTEST_SRAM_TEST_HOOK);

    var state = cpu.ReadState();
    var targetState = LoadState("SMSmemtest_SRAM");
    Assert.True(state.Equals(targetState), "Snapshot mismatch.");
    
    var frame = vdp.ReadFramebuffer();
    var targetFrame = LoadFrame("SMSmemtest_SRAM");
    Assert.True(CompareFrames(frame, targetFrame), "Framebuffer mismatch.");
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

    for (int testCase = 0; testCase < 79; testCase++)
    {
      var steps = 0;
      do
      {
        cpu.Step();
        steps++;

        Assert.False(steps > TEST_CASE_STEP_LIMIT, $"Test case {testCase:D2} exceeded CPU step limit.");
      }
      while (cpu.PC != ZEXDOC_TEST_CASE_HOOK);

      var state = cpu.ReadState();
      var targetState = LoadState($"zexdoc_{testCase:D2}");
      Assert.True(state.Equals(targetState), $"Test case {testCase:D2} snapshot mismatch.");

      var frame = vdp.ReadFramebuffer();
      var targetFrame = LoadFrame($"zexdoc_{testCase:D2}");
      Assert.True(CompareFrames(frame, targetFrame), $"Test case {testCase:D2} framebuffer mismatch.");
    }
  }

  private static byte[] LoadROM(string name) => File.ReadAllBytes($"roms/{name}.sms");
  private static Snapshot LoadState(string name) => Snapshot.ReadFromFile($"states/{name}.state");
  private static byte[] LoadFrame(string name) => File.ReadAllBytes($"frames/{name}.frame");
  private static bool CompareFrames(byte[] framebuffer, byte[] targetbuffer) =>
    StructuralEqualityComparer.Equals(framebuffer, targetbuffer);
}