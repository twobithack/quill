using Quill.UI;

namespace Quill;

public static class Program
{
  static void Main(string[] args)
  {
    if (args == null || args.Length == 0)
      return;

    var romPath = args[0];
    using var quill = new Client(romPath,
                                 scaleFactor: 8,
                                 extraScanlines: 100);
    quill.Run();
  }
}
