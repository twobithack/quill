namespace Sonic
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var cpu = new CPU();
      while (true)
      {
        Console.WriteLine(cpu.ToString());
        cpu.Step();
      }
    }
  }
}
