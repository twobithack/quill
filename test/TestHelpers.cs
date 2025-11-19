using Quill.Core;
using Quill.CPU;
using Quill.IO;
using Quill.Memory;
using Quill.Sound;
using Quill.Video;

namespace Quill.Tests;

internal class TestHelpers
{
  internal static void BuildMachine(string program, out Z80 cpu, out VDP vdp)
  {
    var rom = LoadProgram(program);
    var memory = new Mapper(rom);
    var psg = new PSG(new NullAudioSink());
    vdp = new VDP(new Framebuffer());
    var bus = new Bus(memory, new(), psg, vdp);
    cpu = new Z80(bus);
  }

  internal static byte[] LoadFrame(string name) => File.ReadAllBytes($"Data/Frames/{name}.frame");
  internal static byte[] LoadProgram(string name) => File.ReadAllBytes($"Programs/{name}.sms");
  internal static Snapshot LoadState(string name) => Snapshot.ReadFromFile($"Data/States/{name}.state");
}