namespace Sonic
{
  public class CPU
  {
    // main register set
    private byte _a;
    private byte _b;
    private byte _c;
    private byte _d;
    private byte _e;
    private byte _f;
    private byte _h;
    private byte _l;
    
    // special purpose registers
    private byte _i;
    private byte _r;
    private byte _ixh;
    private byte _ixl;
    private byte _iyh;
    private byte _iyl;
    private Word _pc;
    private Word _sp;

    private Word _ix
    {
      get => new Word(_ixh, _ixl);
      set
      {
        _ixh = value.High;
        _ixl = value.Low;
      }
    }

    private Word _iy
    {
      get => new Word(_iyh, _iyl);
      set
      {
        _iyh = value.High;
        _iyl = value.Low;
      }
    }

    // 16-bit registers
    private Word _af => new Word(_a, _f);
    private Word _bc => new Word(_b, _c);
    private Word _de => new Word(_d, _e);
    private Word _hl => new Word(_h, _l);

    // alternate registers
    private byte _aA;
    private byte _bA;
    private byte _cA;
    private byte _dA;
    private byte _eA;
    private byte _hA;
    private byte _lA;
    private Word _afA => new Word(_aA, _fA);
    private Word _bcA => new Word(_bA, _cA);
    private Word _deA => new Word(_dA, _eA);
    private Word _hlA => new Word(_hA, _lA);
    
    // interrupt flags
    private bool _iff1;
    private bool _iff2;
    
    private Memory _memory;

    private int _cycles;

    public CPU()
    {
      _memory = new Memory();
      // load rom...
      // load ram...
    }

    public Step()
    {
    }
  }
}