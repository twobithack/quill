#!/usr/bin/env bash
set -euo pipefail

OS="$(uname -s)"
ARCH="$(uname -m)"
RID=""

case "$OS" in
  Darwin)
    case "$ARCH" in
      arm64)   RID=osx-arm64   ;;
      x86_64)  RID=osx-x64     ;;
    esac
    ;;
  Linux)
    case "$ARCH" in
      aarch64) RID=linux-arm64 ;;
      x86_64)  RID=linux-x64   ;;
    esac
    ;;
esac

if [[ -z "$RID" ]]; then
  echo "Unsupported platform: $OS-$ARCH" >&2
  exit 1
fi

ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$ROOT/src/Quill.csproj"
OUTDIR="$ROOT/src/bin/aot/$RID"

dotnet publish "$PROJECT" \
  -c Release \
  -r "$RID" \
  -o "$OUTDIR"
  
if [[ "$RID" == osx-* ]]; then
  EXE="$OUTDIR/Quill.app/Contents/MacOS/Quill"
else
  EXE="$OUTDIR/Quill"
fi

exec "$EXE" "$@"