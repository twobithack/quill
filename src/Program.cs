using Quill.UI;
using System.IO;

namespace Quill;

public static class Program
{
  static void Main(string[] args)
  {
    string romPath = @"test\zexdoc.sms";
    if (args != null && args.Length > 0)
      romPath = args[0];

    var rom = File.ReadAllBytes(romPath);
    var romName = Path.GetFileNameWithoutExtension(romPath);
    var saveDirectory = Path.GetDirectoryName(romPath);
    var quill = new Client(rom, 
                           romName, 
                           saveDirectory);
    quill.Run();
  }
}
