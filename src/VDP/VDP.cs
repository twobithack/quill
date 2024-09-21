using Quill.Extensions;

namespace Quill
{
  public unsafe sealed class VDP
  {
    private readonly byte[] _vram = new byte[0x4000];
    private readonly byte[] _cram = new byte[0x20];
    private readonly byte[] _registers = new byte[11];
    private ushort _addressBus;
    private ushort _controlWord;
    private byte _statusRegister;
    private bool _writePending;

    public VDP() {}

    public bool IRQ;
    public byte VCounter;
    public byte HCounter;
    public byte Data;
    public byte Status;
  }
}