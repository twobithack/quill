using Quill.Z80;
using System.Diagnostics;

namespace Quill
{
  public class Quill
  {
    public static void Main(string[] args)
    {
      var cpu = new CPU();
      var cycles = 0;
      var program = ReadROM(@"C:\Source\quill\test\zexdoc.sms");
      cpu.LoadROM(program);

      var sw = new Stopwatch();
      sw.Start();
      while (cycles < program.Count() * 10000)
      {
        cpu.Step();
        cycles++;
      }
      sw.Stop();

      cpu.DumpMemory(@"C:\Users\User\dump.txt");

      Console.WriteLine($"Executed {cycles} instructions in {sw.ElapsedMilliseconds} ms ({(cycles) / (sw.ElapsedMilliseconds / 1000)} per second)");
      Console.Read();
    }

    private static byte[] ReadROM(string path) => File.ReadAllBytes(path);
  }
}
