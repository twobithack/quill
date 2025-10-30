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

Currently, only keyboard input is supported.

| Joypad Button     | Player 1    | Player 2         |
| ----------------- | ----------- | ---------------- |
| Up                | `W`         | `I`              |
| Down              | `S`         | `K`              |
| Left              | `A`         | `J`              |
| Right             | `D`         | `L`              |
| Button 1          | `F`         | `;` (semicolon)  |
| Button 2          | `G`         | `'` (apostrophe) |

| Console Button    | Key         |
| ----------------- | ----------- |
| Pause             | `Space`     |
| Reset             | `Esc`       |

| Function          | Key         |
| ----------------- | ----------- |
| Rewind            | `R` (hold)  |
| Quicksave         | `Enter`     |
| Quickload         | `Backspace` |

## License

Distributed under GPL-3.0 license (see [`LICENSE`](/LICENSE)).