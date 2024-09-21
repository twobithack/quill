using System;

namespace Quill;

unsafe public static class Program
{
  [STAThread]
  static void Main(string[] args)
  {
    if (args == null || args.Length == 0)
      return;
    
    using var game = new Quill(args[0], maskBorder: true);
    game.Run();
  }
}
