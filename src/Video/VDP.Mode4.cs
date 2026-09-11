using System;
using System.Runtime.CompilerServices;

using Quill.Common.Extensions;
using Quill.Video.Definitions;

namespace Quill.Video;

public sealed partial class VDP
{
  #region Constants
  private const int BACKGROUND_ROWS = 28;
  private const int HSCROLL_INHIBIT_END_ROW = 1;
  private const int VSCROLL_INHIBIT_START_COLUMN = 24;
  #endregion

  #region Properties
  private bool ShiftSprites    => TestRegisterBit(0x0, 3);
  private bool BlankLeftColumn => TestRegisterBit(0x0, 5);
  private bool InhibitHScroll  => TestRegisterBit(0x0, 6);
  private bool InhibitVScroll  => TestRegisterBit(0x0, 7);
  
  private bool MagnifySprites  => TestRegisterBit(0x1, 0);
  private bool UseTallSprites  => TestRegisterBit(0x1, 1);

  private ushort SpritePatternGeneratorTableBaseAddress => TestRegisterBit(0x6, 2)
                                                         ? (ushort)0x2000
                                                         : (ushort)0x0000;

  private byte BackdropColorIndex => ((byte)(_registers[0x7] & 0b_1111)).SetBit(4);

  private ushort HScroll => _registers[0x8];

  private bool DisplayMode4 => (_displayMode & DisplayMode.Mode_4) != 0;
  #endregion

  #region Methods
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeMode4Scanline()
  {
    if (!DisplayEnabled || _vCounter > VCOUNTER_ACTIVE)
    {
      BlankMode4Scanline();
    }
    else
    {
      RasterizeMode4Sprites();
      RasterizeMode4Background();
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeMode4Sprites()
  {
    var spriteHeight = TILE_SIZE;
    if (UseTallSprites || MagnifySprites)
      spriteHeight <<= 1;

    var spritesOnScanline = 0;
    for (int spriteIndex = 0; spriteIndex < 64; spriteIndex++)
    {
      int spriteY = _vram[SpriteAttributeTableBaseAddress + spriteIndex];
      if (spriteY == SPRITE_TERMINATOR)
        return;

      spriteY++;
      if (spriteY >= SPRITE_TERMINATOR)
        spriteY -= 0x100;

      if (spriteY > _vCounter ||
          spriteY + spriteHeight <= _vCounter)
        continue;

      spritesOnScanline++;
      if (spritesOnScanline > 8)
        SpriteOverflow = true;

      var attributeOffset = 0x80 + (spriteIndex << 1);
      int spriteX = _vram[SpriteAttributeTableBaseAddress + attributeOffset];
      int patternIndex = _vram[SpriteAttributeTableBaseAddress + attributeOffset + 1];

      if (ShiftSprites)
        spriteX -= TILE_SIZE;

      if (UseTallSprites && spriteY <= _vCounter + TILE_SIZE)
        patternIndex &= 0b_1111_1111_1111_1110;

      var patternRowOffset = (_vCounter - spriteY) << 2;
      var patternOffset = patternIndex << 5;
      var patternAddress = SpritePatternGeneratorTableBaseAddress
                         + patternRowOffset
                         + patternOffset;
      var patternRow = ReadPatternRow(patternAddress);

      var spriteRight = spriteX + TILE_SIZE;
      for (byte patternBit = TILE_SIZE - 1; spriteX < spriteRight; spriteX++, patternBit--)
      {
        if (spriteX >= HORIZONTAL_RESOLUTION)
          break;

        if (spriteX < 0)
          continue;

        if (BlankLeftColumn && spriteX < TILE_SIZE)
          continue;

        var colorIndex = patternRow.GetColorIndex(patternBit);
        if (colorIndex == TRANSPARENT_COLOR_INDEX)
          continue;
        colorIndex += 16;

        if (_spriteMask[spriteX])
        {
          SpriteCollision = true;
          continue;
        }

        SetMode4SpritePixel(spriteX, colorIndex);
      }
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RasterizeMode4Background()
  {
    var applyHorizontalScroll = !InhibitHScroll ||
                                (_vCounter >> TILE_SHIFT) > HSCROLL_INHIBIT_END_ROW;

    for (int screenColumn = 0; screenColumn < BACKGROUND_COLUMNS; screenColumn++)
    {
      ushort nameTableY = _vCounter;
      if (!InhibitVScroll ||
          screenColumn < VSCROLL_INHIBIT_START_COLUMN)
        nameTableY += _vScroll;

      var nameTableRow = nameTableY >> TILE_SHIFT;
      if (nameTableRow >= BACKGROUND_ROWS)
        nameTableRow -= BACKGROUND_ROWS;

      var nameTableColumn = screenColumn;
      if (applyHorizontalScroll)
        nameTableColumn += BACKGROUND_COLUMNS - (HScroll >> TILE_SHIFT);
      nameTableColumn &= BACKGROUND_COLUMNS - 1;

      var nameTableEntryAddress = NameTableBaseAddress
                                + (nameTableRow    << 6)
                                + (nameTableColumn << 1);

      var nameTableEntry = ReadNameTableEntry(nameTableEntryAddress);
      var patternRowIndex = nameTableEntry.VerticalFlip
                          ? 7 - (nameTableY & (TILE_SIZE - 1))
                          : nameTableY & (TILE_SIZE - 1);

      var patternAddress = (nameTableEntry.PatternIndex << 5)
                         + (patternRowIndex << 2);
      var patternRow = ReadPatternRow(patternAddress);

      for (int patternColumn = 0; patternColumn < TILE_SIZE; patternColumn++)
      {
        var screenColumnOffset = nameTableEntry.HorizontalFlip
                               ? 7 - patternColumn
                               : patternColumn;

        var screenX = (nameTableColumn << TILE_SHIFT)
                    + screenColumnOffset;
        if (applyHorizontalScroll)
          screenX += HScroll;
        screenX &= HORIZONTAL_RESOLUTION - 1;

        if (BlankLeftColumn && screenX < TILE_SIZE)
        {
          SetMode4BackgroundPixel(screenX, BackdropColorIndex);
          continue;
        }

        var colorIndex = patternRow.GetColorIndex(7 - patternColumn);
        var spriteHasPriority = colorIndex == TRANSPARENT_COLOR_INDEX ||
                                !nameTableEntry.HighPriority;

        if (spriteHasPriority && _spriteMask[screenX])
          continue;

        if (nameTableEntry.UseSpritePalette)
          colorIndex += 16;

        SetMode4BackgroundPixel(screenX, colorIndex);
      }
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private PatternRow ReadPatternRow(int patternAddress) => new(_vram[patternAddress],
                                                               _vram[patternAddress + 1],
                                                               _vram[patternAddress + 2],
                                                               _vram[patternAddress + 3]);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private NameTableEntry ReadNameTableEntry(int address)
  {
    var data = _vram[address + 1].Concat(_vram[address]);
    return new NameTableEntry(data);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void BlankMode4Scanline()
  {
    var fillColor = _palette[BackdropColorIndex];
    Array.Fill(_scanlinePixels, fillColor);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SetMode4SpritePixel(int x, int paletteIndex)
  {
    _scanlinePixels[x] = _palette[paletteIndex];
    _spriteMask[x] = true;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SetMode4BackgroundPixel(int x, int paletteIndex) => _scanlinePixels[x] = _palette[paletteIndex];

  #endregion
}
