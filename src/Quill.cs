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
      var program = GenerateProgram();

      var sw = new Stopwatch();
      sw.Start();
      while (cycles < program.Count() * 1000)
      {
        cpu.Step();
        //Console.WriteLine(cpu.ToString());
        cycles++;
      }
      sw.Stop();

      Console.WriteLine($"Executed {cycles} instructions in {sw.ElapsedMilliseconds} ms ({(cycles / sw.ElapsedMilliseconds) / 1000} MIPS)");

      cpu.DumpMemory();
      Console.Read();
    }

    private static byte[] GenerateProgram()
    {
      var p = new byte[ushort.MaxValue];
      Array.Fill<byte>(p, 0x87);
      p[0] = 0x3E;
      p[1] = 0x0F;
      return p;
    }
  }
}
