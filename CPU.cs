namespace Sonic
{
  public class CPU
  {
    // general purpose registers
    private byte _a = 0x00;
    private byte _b = 0x00;
    private byte _c = 0x00;
    private byte _d = 0x00;
    private byte _e = 0x00;
    private byte _f = 0x00;
    private byte _h = 0x00;
    private byte _l = 0x00;

    // hardware control registers
    private byte _i = 0x00;
    private byte _r = 0x00;
    
    // interrupt flags
    private bool _iff1 = false;
    private bool _iff2 = false;

    // index registers
    private byte _ixh = 0x00;
    private byte _ixl = 0x00;
    private byte _iyh = 0x00;
    private byte _iyl = 0x00;
    private Word _ix => new Word(_ixh, _ixl);
    private Word _iy => new Word(_iyh, _iyl);

    // register pairs
    private Word _pc = new Word();
    private Word _sp = new Word();
    private Word _af => new Word(_a, _f);
    private Word _bc => new Word(_b, _c);
    private Word _de => new Word(_d, _e);
    private Word _hl => new Word(_h, _l);

    // alternate registers
    private byte _aA = 0x00;
    private byte _bA = 0x00;
    private byte _cA = 0x00;
    private byte _dA = 0x00;
    private byte _eA = 0x00;
    private byte _hA = 0x00;
    private byte _lA = 0x00;
    private Word _afA => new Word(_aA, _fA);
    private Word _bcA => new Word(_bA, _cA);
    private Word _deA => new Word(_dA, _eA);
    private Word _hlA => new Word(_hA, _lA);

    // memory
    private Memory _memory;

    public CPU()
    {
      _memory = new Memory();
    }

    public Step()
    {

    }
  }
}