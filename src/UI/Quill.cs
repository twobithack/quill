using System.IO;

namespace Quill.UI;

public static class Quill
{
  static void Main(string[] args)
  {
    string romPath = @"C:\test\zexdoc.sms";
    if (args != null && args.Length > 0)
      romPath = args[0];

    var rom = File.ReadAllBytes(romPath);
    var romName = Path.GetFileNameWithoutExtension(romPath);
    var saveDirectory = Path.GetDirectoryName(romPath);
    var quill = new Client(rom,
                           romName,
                           saveDirectory,
                           scaleFactor: 5,
                           extraScanlines: 100);
    quill.Run();
  }
}
