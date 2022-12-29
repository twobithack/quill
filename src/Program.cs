using System;

namespace Quill;

unsafe public static class Program
{
  [STAThread]
  static void Main(string[] args)
  {
    string romPath;
    if (args == null || args.Length == 0)
    {
      // Debug.WriteLine("No ROM file has been specified as a parameter.");
      // return;
      romPath = "test/sdsc.sms";
    }
    else
      romPath = args[0];

    using (var game = new Quill(romPath))
      game.Run();
  }
}
