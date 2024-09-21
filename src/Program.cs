using System;
using System.IO;

namespace Quill;

public static class Program
{
  static void Main(string[] args)
  {
    string romPath = @"test\zexdoc.sms";
    if (args != null && args.Length > 0)
      romPath = args[0];

    var rom = ReadROM(romPath);
    using var game = new Quill(rom, cropBorders: true, scaleFactor: 5);
    game.Run();
  }

  private static byte[] ReadROM(string path) => File.ReadAllBytes(path);
}
