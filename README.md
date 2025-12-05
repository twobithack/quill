# Quill &middot; [![build](https://github.com/twobithack/quill/actions/workflows/dotnet.yml/badge.svg)](https://github.com/twobithack/quill/actions/workflows/dotnet.yml)
A cross-platform Sega Master System emulator built on .NET and OpenTK.

![Japanese BIOS screenshot](/docs/screenshots/Japanese%20BIOS.png)

## Features

* Cycle-accurate emulation of Z80 CPU, 315-5124 VDP, and SN76489 PSG
* Savestates: quicksave, quickload, and rewind functionality
* Optional CRT shader, overscan cropping, and aspect ratio correction
* Cross-platform: Windows, Linux, and macOS supported

## Compatibility

Supports most Master System and SG-1000 titles (see [`docs/compatibility.md`](/docs/compatibility.md)). Notable exceptions include titles that:
* Rely on the 315-5246 "SMS2" VDP (Codemasters titles, certain PAL exclusives)
* Require special accessories (3-D Glasses, Light Phaser, Paddle Control)

## Getting Started

### Prerequisites
* .NET SDK 9.0+
* OpenAL runtime (`openal`/`libopenal1`)

### Build & Run

```
$ dotnet run --project src/Quill.csproj -c Release /path/to/rom.sms
```

### Configuration

Configuration options can be found in [`config.json`](/config.json).

### BIOS

Optionally, a BIOS may be provided by placing a file named `bios.sms` in the same directory as the executable. If no BIOS is provided, the emulator will boot the ROM directly.

## Keymap

| Control Pad       | Controller                  | Keyboard (P1) | Keyboard (P2) |
| ----------------- | ----------------------------| ------------- | --------------|
| **D-Pad**         | D-Pad / Left Stick | <kbd>W</kbd> <kbd>A</kbd> <kbd>S</kbd> <kbd>D</kbd> | <kbd>I</kbd> <kbd>J</kbd> <kbd>K</kbd> <kbd>L</kbd> |
| **Button 1**      | <kbd>□</kbd> / <kbd>△</kbd> | <kbd>F</kbd>  | <kbd>;</kbd>  |
| **Button 2**      | <kbd>✕</kbd> / <kbd>○</kbd> | <kbd>G</kbd>  | <kbd>'</kbd>  |

| Console Button    | Controller        | Keyboard             |
| ----------------- | ----------------- | -------------------- |
| **Pause**         | <kbd>START</kbd>  | <kbd>Space</kbd>     |
| **Reset**         | <kbd>SELECT</kbd> | <kbd>Esc</kbd>       |

| Function          | Controller        | Keyboard             |
| ----------------- | ----------------- | -------------------- |
| **Rewind** (hold) | <kbd>L1</kbd>     | <kbd>R</kbd>         |
| **Quickload**     | <kbd>L2</kbd>     | <kbd>Backspace</kbd> |
| **Quicksave**     | <kbd>R2</kbd>     | <kbd>Enter</kbd>     |

## License

Distributed under GPL-3.0 license (see [`LICENSE`](/LICENSE)).