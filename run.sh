#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$ROOT/src/Quill.csproj"

RID="${RID:-$(dotnet --info | sed -n 's/^[[:space:]]*RID:[[:space:]]*//p' | head -n 1)}"
OUT="$ROOT/src/bin/aot/$RID"

dotnet publish "$PROJECT" \
  -c Release \
  -r "$RID" \
  -o "$OUT"
  
if [[ "$RID" == win-* ]]; then
  EXE="$OUT/Quill.exe"
elif [[ "$RID" == osx-* ]]; then
  EXE="$OUT/Quill.app/Contents/MacOS/Quill"
else
  EXE="$OUT/Quill"
fi

exec "$EXE" "$@"