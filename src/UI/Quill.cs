namespace Quill.UI;

public static class Quill
{
  static void Main(string[] args)
  {
    string romPath = @"../test/zexdoc.sms";
    if (args != null && args.Length > 0)
      romPath = args[0];

    using (var quill = new Client(romPath,
                                  scaleFactor: 8,
                                  extraScanlines: 100))
    quill.Run();
  }
}
