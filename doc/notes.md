00000000 = 0
01111111 = 127
10000000 = -128
11111111 = -1

>= 0x80 = negative
< 0x80 = positive

0b_1000_0000_0000_0000

0x0000-0x3FFF : ROM Slot 1
0x4000-0x7FFF : Rom Slot 2
0x8000-0xBFFF : Rom Slot 3 / RAM Slot
0xC000-0xDFFF : RAM
0xE000-0xFFFB : Mirror of 0xC000-0xDFFB
0xFFFC: Memory Control Register ([3]: bank RAM on page 3, [2]: if [3], use 2nd RAM bank)
0xFFFD: page # for slot 1
0xFFFE: page # for slot 2
0xFFFF: page # for slot 3

writing:
0x0000-0x8000 : Not allowed


Ports:

0x7E : Reading: returns VDP V counter. Writing: writes data to Sound Chip
0x7F : Reading: returns VDP H counter. Writing: writes data to Sound Chip (mirror of above)
0xBE : Reading: reads VDP data port: Writing: writes vdp data port
0xBF/0xBD : Reading: Gets VDP statis: Writing: writes to vdp control port
0xDC/0xC0 : Reading: Reads joypad 1. Writing: cannot write to
0xDD/0xC1 : Reading: Reads joypad 2. Writing: cannot write to

Port 0xBD does the same as port 0xBF. Port 0xC0 does the same as 0xDC and finally port 0xC1 does the same as 0xDD  