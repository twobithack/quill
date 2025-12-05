using System;
using System.Runtime.CompilerServices;

using Quill.Common.Extensions;
using Quill.Video.Definitions;

namespace Quill.Video;

public sealed partial class VDP
{
  #region Properties
  private ushort LegacySpritePatternTableAddress => (ushort)((_registers[0x6] & 0b_0000_0111) << 11);
  
  private byte LegacyBlankColor => (byte)(_registers[0x7] & 0b_1111);
  #endregion

  #region Methods
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeLegacyScanline()
  {
    if (!DisplayEnabled || _vCounter > VCOUNTER_ACTIVE)
    {
      BlankLegacyScanline();
    }
    else if (DisplayMode3)
    {
      RasterizeLegacySprites();
      RasterizeMode3Background();
    }
    else
    {
      RasterizeLegacySprites();
      RasterizeMode2Background();
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeLegacySprites()
  {
    var spriteHeight = TILE_SIZE;
    if (StretchSprites)
      spriteHeight <<= 1;

    var spriteCount = 0;
    for (int sprite = 0; sprite < 32; sprite++)
    {
      var baseAddress = SpriteAttributeTableAddress 
                      + (sprite << 2);
      ushort y = _vram[baseAddress];
      
      if (y == DISABLE_SPRITES)
      {
        if (!SpriteOverflow)
          SetLastSpriteIndex(sprite);
        return;
      }

      y++;
      if (y >= DISABLE_SPRITES)
        y -= 0x100;

      if (y > _vCounter ||
          y + spriteHeight <= _vCounter)
        continue;

      var x = _vram[baseAddress + 1];
      var pattern = _vram[baseAddress + 2];
      var color = _vram[baseAddress + 3];

      if (color.TestBit(7))
        x -= 32;

      if (x < 0)
        continue;

      color &= 0b_0000_1111;
      if (color == TRANSPARENT)
        continue;

      spriteCount++;
      if (spriteCount > 4)
      {
        SetLastSpriteIndex(sprite);
        SpriteOverflow = true;
        return;
      }
      else
        SpriteOverflow = false;

      var offset = _vCounter - y;
      if (spriteHeight == TILE_SIZE)
      {
        var address = LegacySpritePatternTableAddress 
                    + (pattern << TILE_SHIFT);
        RasterizeMode2Sprite(address, x, offset, color);
      }
      else
      {
        var address = LegacySpritePatternTableAddress 
                    + ((pattern & 0b_1111_1100) << TILE_SHIFT);
        RasterizeMode2Sprite(address, x, offset, color);
        RasterizeMode2Sprite(address, x + TILE_SIZE, offset + 16, color);
      }
    }
    
    if (!SpriteOverflow)
      SetLastSpriteIndex(31);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeMode2Sprite(int address, int x, int offset, byte color)
  {
    var data = _vram[address + offset];
    for (byte i = 0; i < TILE_SIZE; i++)
    {
      if (x + i >= HORIZONTAL_RESOLUTION)
        return;

      if (_spriteMask[x + i])
      {
        SpriteCollision = true;
        continue;
      }

      if (!data.TestBit(7 - i))
        continue;

      SetLegacySpritePixel(x + i, color);
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeMode2Background()
  {
    var colorMask = (_registers[0x3] << 1) | 1;
    var row       = _vCounter >> 3;
    var rowOffset = _vCounter & (TILE_SIZE - 1);

    var tableAddressOffset = row switch
    {
      < 8   => 0x0,
      < 16  => TestRegisterBit(0x4, 1) ? 0x800  : 0x0,
      _     => TestRegisterBit(0x4, 0) ? 0x1000 : 0x0
    };
    
    for (int column = 0; column < BACKGROUND_COLUMNS; column++)
    {
      var patternIndex = _vram[NameTableAddress + column + (row << 5)];
      var patternAddress = PatternTableAddress
                         + tableAddressOffset
                         + rowOffset
                         + (patternIndex << TILE_SHIFT);

      var colorIndex = patternIndex & colorMask;
      var colorAddress = ColorTableAddress
                       + tableAddressOffset
                       + rowOffset
                       + (colorIndex << TILE_SHIFT);

      var patternData = _vram[patternAddress];
      var colorData = _vram[colorAddress];

      var x = column << TILE_SHIFT;
      var tileEnd = x + TILE_SIZE;
      for (byte i = TILE_SIZE - 1; x < tileEnd; x++, i--)
      {
        if (x >= HORIZONTAL_RESOLUTION)
          return;

        if (_spriteMask[x])
          continue;

        var color = patternData.TestBit(i)
                  ? colorData.HighNibble()
                  : colorData.LowNibble();

        if (color == TRANSPARENT)
          color = LegacyBlankColor;

        SetLegacyBackgroundPixel(x, color);
      }
    }
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeMode3Background()
  {
    var row        = _vCounter >> TILE_SHIFT;
    var rowOffset  = _vCounter & (TILE_SIZE - 1);
    var pairOffset = (row & 0b_11) << 1;

    for (int column = 0; column < BACKGROUND_COLUMNS; column++)
    {
      var patternIndex = _vram[NameTableAddress + column + (row << 5)];
      var patternAddress = PatternTableAddress
                         + (patternIndex << 3)
                         + pairOffset;

      if (rowOffset > 3)
        patternAddress++;

      var patternData = _vram[patternAddress];
      var leftColor   = patternData.HighNibble();
      var rightColor  = patternData.LowNibble();

      var x = column << TILE_SHIFT;
      var tileEnd = x + TILE_SIZE;
      for (int i = 0; x < tileEnd; x++, i++)
      {
        if (x >= HORIZONTAL_RESOLUTION)
          return;

        if (_spriteMask[x])
          continue;

        var color = (i < 4)
                  ? leftColor
                  : rightColor;

        if (color == TRANSPARENT)
          color = LegacyBlankColor;

        SetLegacyBackgroundPixel(x, color);
      }
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void BlankLegacyScanline()
  {
    var fillColor = Color.ToLegacyRGBA(LegacyBlankColor);
    Array.Fill(_scanline, fillColor);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SetLegacySpritePixel(int x, byte color)
  {
    _scanline[x] = Color.ToLegacyRGBA(color);
    _spriteMask[x] = true;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SetLegacyBackgroundPixel(int x, byte color) => _scanline[x] = Color.ToLegacyRGBA(color);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SetLastSpriteIndex(int value)
  {
    _status &= Status.All;
    _status |= (Status)value;
  }
  #endregion
}