namespace Sonic
{
  public class Registers
  {
    // main register set
    public byte A;
    public byte B;
    public byte C;
    public byte D;
    public byte E;
    public byte F;
    public byte H;
    public byte L;
    
    // special purpose registers
    public byte I;
    public byte R;
    public byte IXH;
    public byte IXL;
    public byte IYH;
    public byte IYL;
    public Word PC;
    public Word SP;

    public Word IX
    {
      get => new Word(IXH, IXL);
      set
      {
        IXH = value.High;
        IXL = value.Low;
      }
    }

    public Word IY
    {
      get => new Word(IYH, IYL);
      set
      {
        IYH = value.High;
        IYL = value.Low;
      }
    }

    // registers pairs
    public Word AF => new Word(A, F);
    public Word BC => new Word(B, C);
    public Word DE => new Word(D, E);
    public Word HL => new Word(H, L);

    // alternate registers
    public byte Aa;
    public byte Ba;
    public byte Ca;
    public byte Da;
    public byte Ea;
    public byte Ha;
    public byte La;
    public Word AFa => new Word(Aa, _fA);
    public Word BCa => new Word(Ba, Ca);
    public Word DEa => new Word(Da, Ea);
    public Word HLa => new Word(Ha, La);

    // interrupt flags
    public bool IFF1;
    public bool IFF2;

    // flags
    private Flags _flags => new Flags(F);

    public bool S
    {
      get => _flags[0];
      set => F = _flags.Set(0, value).Byte;
    }

    public bool Z
    {
      get => _flags[1];
      set => F = _flags.Set(1, value).Byte;
    }

    public bool H
    {
      get => _flags[3];
      set => F = _flags.Set(3, value).Byte;
    }

    public bool P
    {
      get => _flags[5];
      set => F = _flags.Set(5, value).Byte;
    }

    public bool N
    {
      get => _flags[6];
      set => F = _flags.Set(6, value).Byte;
    }

    public bool C
    {
      get => _flags[7];
      set => F = _flags.Set(7, value).Byte;
    }

    private string b(bool val) => val ? "1" : "0";

    public override String ToString()
    {
      return $"Registers | A: {A} | B: {B} | C: {C} | D: {D} | E: {E}\r\n" + 
             $"Flags | S: {b(S)} | Z: {b(Z)} | H: {b(H)} | P: {b(P)} | N: {b(N)} | C: {b(C)}";
    }
  }
}