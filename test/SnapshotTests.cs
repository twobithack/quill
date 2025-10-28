using System;

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
  private const int TEST_CASE_STEP_LIMIT = 468281085;
  private const int ZEXDOC_TEST_CASE_HOOK = 0x2C1E;

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
