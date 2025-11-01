# Quill &middot; [![build](https://github.com/twobithack/quill/actions/workflows/dotnet.yml/badge.svg)](https://github.com/twobithack/quill/actions/workflows/dotnet.yml)
A cross-platform Sega Master System emulator built on .NET and OpenTK.

![Screenshot](/docs/screenshots/Sonic%20the%20Hedgehog.png)

## Features

* Cycle-accurate emulation of Z80 CPU, 315-5124 VDP, and SN76489 PSG
* Savestates: quicksave, quickload, and rewind functionality
* Cross-platform: Windows, Linux, and macOS supported

## Compatibility

Supports most Master System and SG-1000 titles (see [`docs/compatibility.md`](/docs/compatibility.md)). 
Notable exceptions include titles that require accessories (3-D Glasses, Light Phaser, Paddle Control), use custom memory mappers (e.g., Codemasters titles), or are PAL-only.

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

## Keymap

| Joypad Button     | Controller                      | Keyboard (P1) | Keyboard (P2) |
| ----------------- | --------------------------------| ------------- | --------------|
| Up                | <kbd>▲</kbd> or <kbd>LS ↑</kbd> | <kbd>W</kbd>  | <kbd>I</kbd>  |
| Down              | <kbd>▼</kbd> or <kbd>LS ↓</kbd> | <kbd>S</kbd>  | <kbd>K</kbd>  |
| Left              | <kbd>◀</kbd> or <kbd>LS ←</kbd> | <kbd>A</kbd>  | <kbd>J</kbd>  |
| Right             | <kbd>▶</kbd> or <kbd>LS →</kbd> | <kbd>D</kbd>  | <kbd>L</kbd>  |
| Button 1          | <kbd>□</kbd> or <kbd>△</kbd>    | <kbd>F</kbd>  | <kbd>;</kbd>  |
| Button 2          | <kbd>✕</kbd> or <kbd>○</kbd>    | <kbd>G</kbd>  | <kbd>'</kbd>  |

| Console Button    | Controller        | Keyboard             |
| ----------------- | ----------------- | -------------------- |
| Pause             | <kbd>START</kbd>  | <kbd>Space</kbd>     |
| Reset             | <kbd>SELECT</kbd> | <kbd>Esc</kbd>       |

| Function          | Controller        | Keyboard             |
| ----------------- | ----------------- | -------------------- |
| Rewind **(hold)** | <kbd>L1</kbd>     | <kbd>R</kbd>         |
| Quickload         | <kbd>L2</kbd>     | <kbd>Backspace</kbd> |
| Quicksave         | <kbd>R2</kbd>     | <kbd>Enter</kbd>     |

## License

Distributed under GPL-3.0 license (see [`LICENSE`](/LICENSE)).