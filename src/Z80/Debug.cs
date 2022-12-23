using System.Text;
using Quill.Extensions;

namespace Quill;

// Implementation of SDSC Debug Console Specification from SMS Power
// https://www.smspower.org/Development/SDSCDebugConsoleSpecification
unsafe public ref partial struct CPU
{
  private struct Text
  {
    public char Character;
    public byte Attributes;

    public Text(byte c, byte a)
    {
      Character = Encoding.ASCII.GetString(new byte[] {c})[0];
      Attributes = a;
    }

    public override string ToString() => Character.ToString();
  }

  private Text[][] _sdsc;
  private int _row = 0;
  private int _col = 0;
  private int _scrollPosition = 0;
  private byte _attribute = 15;
  private char[] _dataFormats = { 'd', 'u', 'x', 'X', 'b', 'a', 's' };

  // control port state 
  private bool _expectAttr = false;
  private bool _expectRow = false;
  private bool _expectCol = false;

  // data port state
  private bool _expectWidth = false;
  private bool _expectFormat = false;
  private bool _expectType0 = false;
  private bool _expectType1 = false;
  private bool _expectByte = false;
  private bool _expectWord0 = false;
  private bool _expectWord1 = false;
  private bool _autoWidth = false;
  private byte _dataWidth = 0;
  private char _dataFormat = ' ';
  private string _dataType = string.Empty;
  private byte _byteParameter = 0;
  private ushort _wordParameter = 0;
  
  private void InitializeSDSC()
  {
    _row = 0;
    _col = 0;
    _sdsc = new Text[25][];

    for (int row = 0; row < 25; row++)
    {
      _sdsc[row] = new Text[80];
      for (int col = 0; col < 80; col++)
        _sdsc[row][col] = new Text((byte)' ', _attribute);
      Console.WriteLine();
    }
    
    _expectWidth = false;
    _expectFormat = false;
    _expectType0 = false;
    _expectType1 = false;
    _expectByte = false;
    _expectWord0 = false;
    _expectWord1 = false;
  }

  private void PrintSDSC()
  {
    for (int row = _scrollPosition; row < 25 + _scrollPosition; row++)
    {
      var output = string.Empty;
      for (int col = 0; col < 80; col++)
        output += _sdsc[row % 25][col].ToString();
      Console.WriteLine(output);
    }
  }

  private void ControlSDSC(byte value)
  {
    if (_expectAttr)
    {
      _attribute = value;
      _expectAttr = false;
    }
    else if (_expectRow)
    {
      _row = value % 25;
      _expectRow = false;
      _expectCol = true;
    }
    else if (_expectCol)
    {
      _col = value % 80;
      _expectCol = false;
    }
    else if (value == 1)
      throw new Exception("Emulation suspended.");
    else if (value == 2)
      InitializeSDSC();
    else if (value == 3)
      _expectAttr = true;
    else if (value == 4)
      _expectRow = true;
    else
      Console.WriteLine($"[ERROR] Invalid command: {value.ToHex()}.");
  }

  private void WriteSDSC(byte value)
  {
    if (_expectWidth)
    {
      _expectWidth = false;
      if (value == 0)
      {
        Console.WriteLine($"[ERROR] Data cannot have a width of 0.");
        return;
      }
      else if (value == 37)
      {
        _sdsc[_row % 25][_col] = new Text(value, _attribute);
        return;
      }
      
      _expectFormat = true;
      if (value >= 48 && value <= 57)
      {
        _autoWidth = false;
        _dataWidth = value;
      }
      else
      {
        _autoWidth = true;
        WriteSDSC(value);
      }
    }
    else if (_expectFormat)
    {
      _expectFormat = false;
      _dataFormat = Encoding.ASCII.GetString(new byte[] {value})[0];
      if (_dataFormats.Contains(_dataFormat))
        _expectType0 = true;
      else
        Console.WriteLine("[ERROR] Invalid data format: " + _dataFormat);
    }
    else if (_expectType0)
    {
      _expectType0 = false;
      _expectType1 = true;
      _dataType = Encoding.ASCII.GetString(new byte[] {value});
    }
    else if (_expectType1)
    {
      _expectType1 = false;
      _dataType += Encoding.ASCII.GetString(new byte[] {value});

      if (!_dataType.EndsWith('b') && 
          (_dataFormat == 'a' || _dataFormat == 's'))
      {
        Console.WriteLine($"[ERROR] Invalid data format/type combination: {_dataFormat}/{_dataType}");
        return;
      }
      
      switch(_dataType)
      {
        case "mw":
        case "mb":
        case "vw":
        case "vb":
          _expectWord0 = true;
          return;

        case "pr":
        case "vr":
          _expectByte = true;
          return;

        default:
          Console.WriteLine("[ERROR] Invalid data type: " + _dataType);
          return;
      }
    }
    else if (_expectByte)
    {
      _expectByte = false;
      _byteParameter = value;
      PrintData();
    }
    else if (_expectWord0)
    {
      _expectWord0 = false;
      _expectWord1 = true;
      _byteParameter = value;
    }
    else if (_expectWord1)
    {
      _expectWord1 = false;
      _wordParameter = value.Concat(_byteParameter);
      PrintData();
    }
    else if (value == 10)
    {
      _col = 0;
      if (_row == 24)
        _scrollPosition++;
      else
        _row++;
        
      PrintSDSC();
    }
    else if (value == 13)
    {
      _col = 0;
      PrintSDSC();
    }
    else if (value == 37)
    {
      _expectWidth = true;
    }
    else if (value < 32 || value > 127)
    {
      Console.WriteLine($"[ERROR] Undefined character: {value.ToHex()} at instruction {_instruction}");
    }
    else 
    {
      WriteCharacter(value);
    }
    PrintSDSC();
  }

  private void WriteCharacter(byte value) => WriteCharacter(Encoding.ASCII.GetString(new byte[]{value})[0]);
  private void WriteCharacter(char value)
  {
    _sdsc[_row % 25][_col].Character = value;
    _col++;

    if (_col == 80) 
    {
      _col = 0;
      if (_row == 24)
        _scrollPosition++;
      else
        _row++;
        
      PrintSDSC();
    }
  }
  
  private void PrintData()
  {
    var formatted = _dataType switch
    {
      "mb" => FormatMemoryByte(),
      "mw" => FormatMemoryWord(),
      "pr" => FormatRegister(),
      "vb" => FormatVramByte(),
      "vw" => FormatVramword(),
      "vr" => PrintVDPRegister()
    };

    if (_autoWidth)
      _dataWidth = (byte)formatted.Length;

    var padded = string.Empty;
    if (_dataWidth < formatted.Length)
      padded = formatted.Substring(0, _dataWidth);
    else
    {
      var padding = _dataWidth - formatted.Length;
      for (int p = 0; p < padding; p++)
        padded = ' ' + padded;
    }

    for (int i = 0; i < _dataWidth; i++)
      WriteCharacter(padded[i]);
    
    PrintSDSC();
  }

  private string FormatMemoryByte()
  {
    var value = _memory.ReadByte(_wordParameter++);
    var display = string.Empty;
    var bytes = new byte[]{value};

    switch (_dataFormat)
    {
      case 'd': return unchecked((int)value).ToString();
      case 'u': return ((int)value).ToString();
      case 'x': 
      case 'X': return value.ToHex();
      case 'b': return Convert.ToString(value, 2);
      case 'a': 
        if (_autoWidth) 
          _dataWidth = 1;
        for (int i = 1; i < _dataWidth; i++)
          bytes[i] = _memory.ReadByte(_wordParameter++);
        return Encoding.ASCII.GetString(bytes);

      case 's':
        if (_autoWidth) 
          _dataWidth = byte.MaxValue;
        for (int i = 1; i < _dataWidth; i++)
        {
          value = _memory.ReadByte(_wordParameter++);
          if (value == 0)
            break;
          bytes[i] = value;
        }
        return Encoding.ASCII.GetString(bytes);
          
      default:
        return value.ToHex();
    }
  }

  private string FormatMemoryWord()
  {
    var value = _memory.ReadWord(_wordParameter);
    var display = string.Empty;

    switch (_dataFormat)
    {
      case 'd': return unchecked((int)value).ToString();
      case 'u': return ((int)value).ToString();
      case 'x': 
      case 'X': return value.ToHex();
      case 'b': return Convert.ToString(value, 2);
      default:
        return value.ToHex();
    }
  }

  private string FormatRegister()
  {
    // TODO: formatting
    return _wordParameter switch
    {
      0x00 or 0x62 => _b.ToHex(),
      0x01 or 0x63 => _c.ToHex(),
      0x02 or 0x64 => _d.ToHex(),
      0x03 or 0x65 => _e.ToHex(),
      0x04 or 0x68 => _h.ToHex(),
      0x05 or 0x6C => _l.ToHex(),
      0x06 or 0x66 => ((byte)_flags).ToHex(),
      0x07 or 0x61 => _a.ToHex(),
      0x08 or 0x70 => _pc.ToHex(),
      0x09 or 0x73 => _sp.ToHex(),
      0x0A or 0x78 => _ix.ToHex(),
      0x0B or 0x79 => _iy.ToHex(),
      0x0C or 0x42 => _bc.ToHex(),
      0x0D or 0x44 => _de.ToHex(),
      0x0E or 0x48 => _hl.ToHex(),
      0x0F or 0x63 => _af.ToHex(),
      0x10 or 0x72 => _r.ToHex(),
      0x11 or 0x69 => _r.ToHex(),
      0x12 => _bcShadow.ToHex(),
      0x13 => _deShadow.ToHex(),
      0x14 => _hlShadow.ToHex(),
      0x15 => _afShadow.ToHex(),
    };
  }

  private string FormatVramByte()
  {
    throw new Exception("VDP not implemented");
  }

  private string FormatVramword()
  {
    throw new Exception("VDP not implemented");
  }

  private string PrintVDPRegister()
  {
    throw new Exception("VDP not implemented");
  }
}