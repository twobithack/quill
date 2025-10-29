using Quill.Common.Extensions;
using Quill.Core;
using Quill.CPU;
using Quill.IO;
using Quill.Memory;
using Quill.Sound;
using Quill.Video;
using Xunit;

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
    var rom = File.ReadAllBytes("roms/SMSmemtest.sms");
    var memory = new Mapper(rom);
    var psg = new PSG(_ => { });
    var bus = new Bus(memory, new(), psg, new());
    var cpu = new Z80(bus);

    var steps = 0;
    do
    {
      cpu.Step();
      steps++;

      Assert.False(steps > TEST_CASE_STEP_LIMIT, $"SMSmemtest RAM test exceeded CPU step limit.");
    }
    while (cpu.PC != SMSMEMTEST_RAM_TEST_HOOK);

    var targetState = Snapshot.ReadFromFile($"states/SMSmemtest_RAM.state");
    Assert.True(cpu.DumpState().Equals(targetState), $"SMSmemtest RAM test snapshot mismatch.");
  }

  [Fact]
  public void SMSmemtest_VRAM()
  {
    var rom = File.ReadAllBytes("roms/SMSmemtest.sms");
    var memory = new Mapper(rom);
    var psg = new PSG(_ => { });
    var bus = new Bus(memory, new(), psg, new());
    var cpu = new Z80(bus);

    var initialState = Snapshot.ReadFromFile("states/SMSmemtest_VRAM.state");
    cpu.LoadState(initialState);

    var steps = 0;
    do
    {
      cpu.Step();
      steps++;

      Assert.False(steps > TEST_CASE_STEP_LIMIT, $"SMSmemtest VRAM test exceeded CPU step limit.");
    }
    while (cpu.PC != SMSMEMTEST_VRAM_TEST_HOOK);

    var targetState = Snapshot.ReadFromFile($"states/SMSmemtest_VRAM0.state");
    Assert.True(cpu.DumpState().Equals(targetState), $"SMSmemtest VRAM test snapshot mismatch.");
  }
  
  [Fact]
  public void SMSmemtest_SRAM()
  {
    var rom = File.ReadAllBytes("roms/SMSmemtest.sms");
    var memory = new Mapper(rom);
    var psg = new PSG(_ => { });
    var bus = new Bus(memory, new(), psg, new());
    var cpu = new Z80(bus);

    var initialState = Snapshot.ReadFromFile("states/SMSmemtest_SRAM.state");
    cpu.LoadState(initialState);

    var steps = 0;
    do
    {
      cpu.Step();
      steps++;

      Assert.False(steps > TEST_CASE_STEP_LIMIT, $"SMSmemtest SRAM test exceeded CPU step limit.");
    }
    while (cpu.PC != SMSMEMTEST_SRAM_TEST_HOOK);

    var targetState = Snapshot.ReadFromFile($"states/SMSmemtest_SRAM0.state");
    Assert.True(cpu.DumpState().Equals(targetState), $"SMSmemtest SRAM test snapshot mismatch.");
  }

  [Fact]
  public void ZexDoc()
  {
    var rom = File.ReadAllBytes("roms/zexdoc.sms");
    var memory = new Mapper(rom);
    var psg = new PSG(_ => { });
    var bus = new Bus(memory, new(), psg, new());
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
      
      var targetState = Snapshot.ReadFromFile($"states/zexdoc_{testCase:D2}.state");
      Assert.True(cpu.DumpState().Equals(targetState), $"Test case {testCase:D2} snapshot mismatch.");
    }
  }
}
