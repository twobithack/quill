# Timing
* System clock rate: 53.693175 MHz (NTSC)
* System clock cycles per frame: 896,040 (NTSC)
* System clock cycles per scanline: 3420
* CPU clock rate: 3.579545 MHz (NTSC) (clock / 15)
* VDP clock rate: 10.738635 Mhz (NTSC) (clock / 5)
* Pixel clock rate: 5.3693175 MHz (NTSC) (clock / 10)

# Ports
0x7E : Reading: returns VDP V counter. Writing: writes data to Sound Chip
0x7F : Reading: returns VDP H counter. Writing: writes data to Sound Chip (mirror of above)
0xBE : Reading: reads VDP data port: Writing: writes vdp data port
0xBF/0xBD : Reading: Gets VDP status: Writing: writes to vdp control port
0xDC/0xC0 : Reading: Reads joypad 1. Writing: cannot write to
0xDD/0xC1 : Reading: Reads joypad 2. Writing: cannot write to

# VDP Registers

## Register 0x0
Bit7 = If set then vertial scrolling for columns 24-31 are disabled
Bit6 = If set then horizontal scrolling for colums 0-1 are disabled
Bit5 = If set then column 0 is set to the colour of register 0x7
Bit4 = If set then line interrupt is enabled
Bit3 = If set sprites are moved left by 8 pixels
Bit2 = If set use Mode 4
Bit1 = If set use Mode 2. Must also be set for mode4 to change screen resolution

## Register 0x1
Bit6 = If set the screen is enabled
Bit5 = If set vsync interrupts are enabled
Bit4 = If set active display has 224 (medium) scanlines. Reg 0 bit1 must be set
Bit3 = If set active display has 240 (large) scanlines. Reg0 bit1 must be set
Bit1 = If set sprites are 16x16 otherwise 8x8
Bit0 = If set sprites are zoomed (double height)

## Register 0x2
Bit3 = Bit13 of the name base table address
Bit2 = Bit12 of the name base table address
Bit1 = Bit11 of the name base table address if resolution is "small" otherwise unused

## Register 0x5
Bit 6 = Bit13 of sprite info base table
Bit 5 = Bit12 of sprite info base table
Bit 4 = Bit11 of sprite info base table
Bit 3 = Bit10 of sprite info base table
Bit 2 = Bit9 of sprite info base table
Bit 1 = Bit8 of sprite info base table

## Register 0x6
Bit 2 = If set sprites use tiles in memory 0x2000 (tiles 256..511), else memory 0x0 (tiles 0 - 256)

## Register 0x7
Bits 3-0 = Defines the colour to use for the overscan border

## Register 0x8
Background X Scrolling position

## Register 0x9
Background Y Scrolling position

## Register 0xA
The entire 8 bit register is what the line counter should be set to


# VDP Regions

## NTSC small (256x192)
* 0-191 = active display
* 192-255 = inactive display
* Vcounter values = 0x0-0xDA, 0xD5-0xFF 

## NTSC medium (256x224)
* 0-223 = active display
* 224-255 = inactive display
* VCounter values = 0x0-0xEA, 0x0E5-0xFF

## NTSC large
* Not supported