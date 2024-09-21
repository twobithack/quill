namespace Sonic
{
  public class Sonic
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
