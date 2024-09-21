using Quill.Extensions;

namespace Quill;

unsafe public ref struct VDP
{
  private readonly byte[] _vram = new byte[0x4000];
  private readonly byte[] _cram = new byte[0x20];
  private readonly byte[] _registers = new byte[11];
  private ushort _addressBus = 0x0000;
  private ushort _controlWord = 0x0000;
  private byte _statusRegister = 0x00;
  private bool _writePending = false;

  public bool IRQ = false;
  public byte VCounter = 0x00;
  public byte HCounter = 0x00;
  public byte Control = 0x00;
  public byte Data = 0x00;
  public byte Status = 0x00;

  public VDP() {}

  public void AcknowledgeIRQ()
  {
    IRQ = false;
  }
}