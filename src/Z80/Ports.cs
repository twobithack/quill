namespace Quill
{
  public unsafe sealed class Ports
  {
    public class Port
    {
      public byte Data;
      public bool NMI;
      public bool IRQ;
    }

    private Port[] _ports;
    
    public Ports()
    {
      _ports = new Port[0xFF];
      Array.Fill(_ports, new Port());
    }

    public byte ReadPort(byte port) => _ports[port].Data;

    public void WritePort(byte port, byte value)
    {
      
    }
  }
}