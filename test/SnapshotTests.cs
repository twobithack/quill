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
  public void SerializationTest()
  {
    TestHelpers.BuildMachine("SMSmemtest", out var cpu, out _);

    var loadedState = TestHelpers.LoadState("SMSmemtest_RAM");
    cpu.LoadState(loadedState);

    var savedState = cpu.ReadState();
    Assert.Equal(loadedState, savedState);
  }

  [Fact]
  public void SMSmemtestRAM()
  {
    TestHelpers.BuildMachine("SMSmemtest", out var cpu, out var vdp);

    var steps = TEST_CASE_STEP_LIMIT;
    do
    {
      cpu.Step();
      steps--;

      Assert.True(steps > 0, "CPU step limit exceeded.");
    }
    while (cpu.PC != SMSMEMTEST_RAM_TEST_HOOK);

    var state = cpu.ReadState();
    var targetState = TestHelpers.LoadState("SMSmemtest_RAM");
    Assert.Equal(state, targetState);

    var frame = vdp.ReadFramebuffer();
    var targetFrame = TestHelpers.LoadFrame("SMSmemtest_RAM");
    Assert.Equal(frame, targetFrame);
  }

  [Fact]
  public void SMSmemtestVRAM()
  {
    TestHelpers.BuildMachine("SMSmemtest", out var cpu, out var vdp);

    var initialState = TestHelpers.LoadState("SMSmemtest_VRAM_init");
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
    var targetState = TestHelpers.LoadState("SMSmemtest_VRAM");
    Assert.Equal(state, targetState);

    var frame = vdp.ReadFramebuffer();
    var targetFrame = TestHelpers.LoadFrame("SMSmemtest_VRAM");
    Assert.Equal(frame, targetFrame);
  }

  [Fact]
  public void SMSmemtestSRAM()
  {
    TestHelpers.BuildMachine("SMSmemtest", out var cpu, out var vdp);

    var initialState = TestHelpers.LoadState("SMSmemtest_SRAM_init");
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
    var targetState = TestHelpers.LoadState("SMSmemtest_SRAM");
    Assert.Equal(state, targetState);

    var frame = vdp.ReadFramebuffer();
    var targetFrame = TestHelpers.LoadFrame("SMSmemtest_SRAM");
    Assert.Equal(frame, targetFrame);
  }

  [Fact]
  public void Zexdoc()
  {
    TestHelpers.BuildMachine("zexdoc", out var cpu, out var vdp);

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
      var targetState = TestHelpers.LoadState($"zexdoc_{testCase:D2}");
      Assert.Equal(state, targetState);

      var frame = vdp.ReadFramebuffer();
      var targetFrame = TestHelpers.LoadFrame($"zexdoc_{testCase:D2}");
      Assert.Equal(frame, targetFrame);
    }
  }

  [Fact, Trait("Category", "Long")]
  public void ZexdocFull()
  {
    TestHelpers.BuildMachine("zexdoc", out var cpu, out var vdp);

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
      var targetState = TestHelpers.LoadState($"zexdoc_{testCase:D2}");
      Assert.Equal(state, targetState);

      var frame = vdp.ReadFramebuffer();
      var targetFrame = TestHelpers.LoadFrame($"zexdoc_{testCase:D2}");
      Assert.Equal(frame, targetFrame);
    }
  }
}