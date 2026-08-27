#!/usr/bin/env bash
# Format (or check) game C# with the repo-local CSharpier tool.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

if ! command -v dotnet >/dev/null 2>&1; then
  if [[ -x "${HOME}/.dotnet/dotnet" ]]; then
    export PATH="${HOME}/.dotnet:${PATH}"
  else
    echo "dotnet SDK not found. Install .NET 8+ and retry." >&2
    exit 1
  fi
fi

dotnet tool restore

TARGETS=(Assets/Scripts Assets/Tests)
MODE="${1:-format}"

case "$MODE" in
  format|fix)
    echo "CSharpier: formatting ${TARGETS[*]}"
    dotnet csharpier format "${TARGETS[@]}"
    ;;
  check)
    echo "CSharpier: checking ${TARGETS[*]}"
    dotnet csharpier check "${TARGETS[@]}"
    ;;
  *)
    echo "Usage: $0 [format|check]" >&2
    exit 2
    ;;
esac
