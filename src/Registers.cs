using Sonic.Definitions;
using Sonic.Extensions;

namespace Sonic
{
  public partial class CPU
  {
    // special-purpose registers
    private ushort PC;
    private ushort SP;
    private ushort IX;
    private ushort IY;
    private byte I;
    private byte R;
    private byte A;
    private byte F;

    // general-purpose registers
    private byte B;
    private byte C;
    private byte D;
    private byte E;
    private byte H;
    private byte L;

    // complimentary registers
    private byte Ap;
    private byte Bp;
    private byte Cp;
    private byte Dp;
    private byte Ep;
    private byte Fp;
    private byte Hp;
    private byte Lp;

    // interrupt flags
    private bool Iff1;
    private bool Iff2;

    private ushort _af;
    // registers pairs
    private ushort AF
    {
      get => Util.ConcatBytes(A, F);
      set => value.ExtractBytes(ref A, ref F);
    }

    private ushort BC
    {
      get => Util.ConcatBytes(B, C);
      set => value.ExtractBytes(ref B, ref C);
    }

    private ushort DE
    {
      get => Util.ConcatBytes(D, E);
      set => value.ExtractBytes(ref D, ref E);
    }

    private ushort HL
    {
      get => Util.ConcatBytes(H, L);
      set => value.ExtractBytes(ref H, ref L);
    }

    // alternate register pairs
    private ushort AFp
    {
      get => Util.ConcatBytes(Ap, Fp);
      set => value.ExtractBytes(ref Ap, ref Fp);
    }

    private ushort BCp
    {
      get => Util.ConcatBytes(Bp, Cp);
      set => value.ExtractBytes(ref Bp, ref Cp);
    }
    
    private ushort DEp
    {
      get => Util.ConcatBytes(Bp, Cp);
      set => value.ExtractBytes(ref Bp, ref Cp);
    }

    private ushort HLp
    {
      get => Util.ConcatBytes(Bp, Cp);
      set => value.ExtractBytes(ref Bp, ref Cp);
    }
  }
}