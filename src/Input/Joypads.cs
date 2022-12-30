using Quill.Input.Definitions;

namespace Quill.Input;

public class Joypads
{
  private PortA _portA;
  private PortB _portB;

  public Joypads()
  {
    _portA = PortA.All;
    _portB = PortB.All;
  }

  public byte ReadPortA() => (byte)_portA;
  public byte ReadPortB() => (byte)_portB;

  public bool Joy1Up
  {
    set
    {
      if (value)
        _portA &= ~PortA.Joy1Up;
      else
        _portA |= PortA.Joy1Up;
    }
  }

  public bool Joy1Down
  {
    set
    {
      if (value)
        _portA &= ~PortA.Joy1Down;
      else
        _portA |= PortA.Joy1Down;
    }
  }

  public bool Joy1Left
  {
    set
    {
      if (value)
        _portA &= ~PortA.Joy1Left;
      else
        _portA |= PortA.Joy1Left;
    }
  }

  public bool Joy1Right
  {
    set
    {
      if (value)
        _portA &= ~PortA.Joy1Right;
      else
        _portA |= PortA.Joy1Right;
    }
  }

  public bool Joy1FireA
  {
    set
    {
      if (value)
        _portA &= ~PortA.Joy1FireA;
      else
        _portA |= PortA.Joy1FireA;
    }
  }

  public bool Joy1FireB
  {
    set
    {
      if (value)
        _portA &= ~PortA.Joy1FireB;
      else
        _portA |= PortA.Joy1FireB;
    }
  }
}