namespace Quill.Tests;

public class SoundTests
{
  private const int TEST_CASE_STEPS = 100000;

  [Fact]
  public void SN76489TestRomF4()
  {
    TestHelpers.BuildMachine("SN76489_TestRom_NTSC", out var cpu, out _);

    var initialState = TestHelpers.LoadState("SN76489_TestRom_F4");
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
    TestHelpers.BuildMachine("SN76489_TestRom_NTSC", out var cpu, out _);

    var initialState = TestHelpers.LoadState("SN76489_TestRom_A4");
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
    TestHelpers.BuildMachine("SN76489_TestRom_NTSC", out var cpu, out _);

    var initialState = TestHelpers.LoadState("SN76489_TestRom_C5");
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
    TestHelpers.BuildMachine("SN76489_TestRom_NTSC", out var cpu, out _);

    var initialState = TestHelpers.LoadState("SN76489_TestRom_E5");
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
}