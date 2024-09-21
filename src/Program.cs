using Quill.UI;

namespace Quill;

public static class Program
{
  static void Main(string[] args)
  {
    string romPath = @"../test/zexdoc.sms";
    if (args != null && args.Length > 0)
        romPath = args[0];

    using var quill = new Client(romPath,
                                 scaleFactor: 5,
                                 extraScanlines: 0);
    quill.Run();
  }
}
