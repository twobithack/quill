using System;
using System.Runtime.CompilerServices;

using Quill.Common.Extensions;
using Quill.Video.Definitions;

namespace Quill.Video;

public sealed partial class VDP
{
  #region Properties
  private ushort LegacyColorTableBaseAddress => TestRegisterBit(0x3, 7)
                                              ? (ushort)0x2000
                                              : (ushort)0x0000;

  private ushort LegacyPatternGeneratorTableBaseAddress => TestRegisterBit(0x4, 2)
                                                         ? (ushort)0x2000
                                                         : (ushort)0x0000;

  private ushort LegacySpritePatternGeneratorTableBaseAddress => (ushort)((_registers[0x6] & 0b_0000_0111) << 11);
  
  private byte LegacyBackdropColorIndex => (byte)(_registers[0x7] & 0b_1111);
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

    var spritesOnScanline = 0;
    for (int spriteIndex = 0; spriteIndex < 32; spriteIndex++)
    {
      var attributeAddress = SpriteAttributeTableBaseAddress
                           + (spriteIndex << 2);
      int spriteY = _vram[attributeAddress];

      if (spriteY == SPRITE_TERMINATOR)
      {
        if (!SpriteOverflow)
          SetLastSpriteIndex(spriteIndex);
        return;
      }

      spriteY++;
      if (spriteY >= SPRITE_TERMINATOR)
        spriteY -= 0x100;

      if (spriteY > _vCounter ||
          spriteY + spriteHeight <= _vCounter)
        continue;

      int spriteX = _vram[attributeAddress + 1];
      var patternIndex = _vram[attributeAddress + 2];
      var colorAttribute = _vram[attributeAddress + 3];

      if (colorAttribute.TestBit(7))
        spriteX -= 32;

      if (spriteX < 0)
        continue;

      var colorIndex = (byte)(colorAttribute & 0b_0000_1111);
      if (colorIndex == TRANSPARENT_COLOR_INDEX)
        continue;

      spritesOnScanline++;
      if (spritesOnScanline > 4)
      {
        SetLastSpriteIndex(spriteIndex);
        SpriteOverflow = true;
        return;
      }
      else
        SpriteOverflow = false;

      var patternRow = _vCounter - spriteY;
      if (spriteHeight == TILE_SIZE)
      {
        var patternAddress = LegacySpritePatternGeneratorTableBaseAddress
                           + (patternIndex << TILE_SHIFT);
        RasterizeMode2Sprite(patternAddress, spriteX, patternRow, colorIndex);
      }
      else
      {
        var patternAddress = LegacySpritePatternGeneratorTableBaseAddress
                           + ((patternIndex & 0b_1111_1100) << TILE_SHIFT);
        RasterizeMode2Sprite(patternAddress, spriteX, patternRow, colorIndex);
        RasterizeMode2Sprite(patternAddress, spriteX + TILE_SIZE, patternRow + 16, colorIndex);
      }
    }

    if (!SpriteOverflow)
      SetLastSpriteIndex(31);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeMode2Sprite(int patternAddress, int spriteX, int patternRow, byte colorIndex)
  {
    var patternData = _vram[patternAddress + patternRow];
    for (byte pixelOffset = 0; pixelOffset < TILE_SIZE; pixelOffset++)
    {
      var screenX = spriteX + pixelOffset;
      if (screenX >= HORIZONTAL_RESOLUTION)
        return;

      if (_spriteMask[screenX])
      {
        SpriteCollision = true;
        continue;
      }

      if (!patternData.TestBit(7 - pixelOffset))
        continue;

      SetLegacySpritePixel(screenX, colorIndex);
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeMode2Background()
  {
    var colorTableMask = (_registers[0x3] << 1) | 1;
    var nameTableRow = _vCounter >> TILE_SHIFT;
    var patternRow = _vCounter & (TILE_SIZE - 1);

    var tableSectionOffset = nameTableRow switch
    {
      < 8  => 0x0,
      < 16 => TestRegisterBit(0x4, 1) ? 0x800  : 0x0,
      _    => TestRegisterBit(0x4, 0) ? 0x1000 : 0x0
    };

    for (int nameTableColumn = 0; nameTableColumn < BACKGROUND_COLUMNS; nameTableColumn++)
    {
      var patternIndex = _vram[NameTableBaseAddress + nameTableColumn + (nameTableRow << 5)];
      var patternAddress = LegacyPatternGeneratorTableBaseAddress
                         + tableSectionOffset
                         + patternRow
                         + (patternIndex << TILE_SHIFT);

      var colorTableIndex = patternIndex & colorTableMask;
      var colorAddress = LegacyColorTableBaseAddress
                       + tableSectionOffset
                       + patternRow
                       + (colorTableIndex << TILE_SHIFT);

      var patternData = _vram[patternAddress];
      var colorPair = _vram[colorAddress];

      var screenX = nameTableColumn << TILE_SHIFT;
      var characterRight = screenX + TILE_SIZE;
      for (byte patternBit = TILE_SIZE - 1; screenX < characterRight; screenX++, patternBit--)
      {
        if (screenX >= HORIZONTAL_RESOLUTION)
          return;

        if (_spriteMask[screenX])
          continue;

        var colorIndex = patternData.TestBit(patternBit)
                       ? colorPair.HighNibble()
                       : colorPair.LowNibble();

        if (colorIndex == TRANSPARENT_COLOR_INDEX)
          colorIndex = LegacyBackdropColorIndex;

        SetLegacyBackgroundPixel(screenX, colorIndex);
      }
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeMode3Background()
  {
    var nameTableRow = _vCounter >> TILE_SHIFT;
    var patternRow = _vCounter & (TILE_SIZE - 1);
    var colorPairOffset = (nameTableRow & 0b_11) << 1;

    for (int nameTableColumn = 0; nameTableColumn < BACKGROUND_COLUMNS; nameTableColumn++)
    {
      var patternIndex = _vram[NameTableBaseAddress + nameTableColumn + (nameTableRow << 5)];
      var colorPairAddress = LegacyPatternGeneratorTableBaseAddress
                           + (patternIndex << TILE_SHIFT)
                           + colorPairOffset;

      if (patternRow > 3)
        colorPairAddress++;

      var colorPair = _vram[colorPairAddress];
      var leftColorIndex = colorPair.HighNibble();
      var rightColorIndex = colorPair.LowNibble();

      var screenX = nameTableColumn << TILE_SHIFT;
      var characterRight = screenX + TILE_SIZE;
      for (int pixelOffset = 0; screenX < characterRight; screenX++, pixelOffset++)
      {
        if (screenX >= HORIZONTAL_RESOLUTION)
          return;

        if (_spriteMask[screenX])
          continue;

        var colorIndex = pixelOffset < 4
                       ? leftColorIndex
                       : rightColorIndex;

        if (colorIndex == TRANSPARENT_COLOR_INDEX)
          colorIndex = LegacyBackdropColorIndex;

        SetLegacyBackgroundPixel(screenX, colorIndex);
      }
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void BlankLegacyScanline()
  {
    var fillColor = Color.ToLegacyRGBA(LegacyBackdropColorIndex);
    Array.Fill(_scanlinePixels, fillColor);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SetLegacySpritePixel(int screenX, byte colorIndex)
  {
    _scanlinePixels[screenX] = Color.ToLegacyRGBA(colorIndex);
    _spriteMask[screenX] = true;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SetLegacyBackgroundPixel(int screenX, byte colorIndex) => _scanlinePixels[screenX] = Color.ToLegacyRGBA(colorIndex);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SetLastSpriteIndex(int value)
  {
    _status &= Status.Flags;
    _status |= (Status)value;
  }
  #endregion
}