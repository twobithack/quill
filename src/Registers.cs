using Sonic.Definitions;
using Sonic.Extensions;

namespace Sonic
{
  public class Registers
  {
    // instruction register
    public byte Instruction;

    // control registers
    public ushort PC;
    public ushort SP;
    public byte I;
    public byte R;

    // main register set
    public byte A;
    public byte B;
    public byte C;
    public byte D;
    public byte E;
    public byte F;
    public byte H;
    public byte L;
    public byte IXH;
    public byte IXL;
    public byte IYH;
    public byte IYL;

    // alternate register set
    public byte Ap;
    public byte Bp;
    public byte Cp;
    public byte Dp;
    public byte Ep;
    public byte Fp;
    public byte Hp;
    public byte Lp;

    // interrupt flags
    public bool Iff1;
    public bool Iff2;

    // registers pairs
    public ushort AF
    {
      get => Util.ConcatBytes(A, F);
      set => value.ExtractBytes(ref A, ref F);
    }

    public ushort BC
    {
      get => Util.ConcatBytes(B, C);
      set => value.ExtractBytes(ref B, ref C);
    }

    public ushort DE
    {
      get => Util.ConcatBytes(D, E);
      set => value.ExtractBytes(ref D, ref E);
    }

    public ushort HL
    {
      get => Util.ConcatBytes(H, L);
      set => value.ExtractBytes(ref H, ref L);
    }

    public ushort IX
    {
      get => Util.ConcatBytes(IXH, IXL);
      set => value.ExtractBytes(ref IXH, ref IXL);
    }
    
    public ushort IY
    {
      get => Util.ConcatBytes(IYH, IYL);
      set => value.ExtractBytes(ref IYH, ref IYL);
    }

    // alternate register pairs
    public ushort AFp
    {
      get => Util.ConcatBytes(Ap, Fp);
      set => value.ExtractBytes(ref Ap, ref Fp);
    }

    public ushort BCp
    {
      get => Util.ConcatBytes(Bp, Cp);
      set => value.ExtractBytes(ref Bp, ref Cp);
    }
    
    public ushort DEp
    {
      get => Util.ConcatBytes(Bp, Cp);
      set => value.ExtractBytes(ref Bp, ref Cp);
    }

    public ushort HLp
    {
      get => Util.ConcatBytes(Bp, Cp);
      set => value.ExtractBytes(ref Bp, ref Cp);
    }

    public override String ToString()
    {
      return  $"╒══════════╤════════════╤════════════╤════════════╤════════════╕\r\n" +
              $"│Registers │  AF: {AF.ToHex()} │  BC: {BC.ToHex()} │  DE: {DE.ToHex()} │  HL: {HL.ToHex()} │\r\n" +
              $"│          │ AF': {AFp.ToHex()} │ BC': {BCp.ToHex()} │ DE': {DEp.ToHex()} │ HL': {HLp.ToHex()} │\r\n" +
              $"│          │  IX: {IX.ToHex()} │  IY: {IY.ToHex()} │  PC: {PC.ToHex()} │  SP: {SP.ToHex()} │\r\n" +
              $"╘══════════╧════════════╧════════════╧════════════╧════════════╛";
    }
  }
}