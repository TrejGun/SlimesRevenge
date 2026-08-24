#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
UNITY="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/2021.3.24f1/Unity.app/Contents/MacOS/Unity}"
TARGET="${1:-}"

if [[ ! -x "$UNITY" ]]; then
  echo "Unity Editor not found at: $UNITY" >&2
  echo "Install Unity 2021.3.24f1 or set UNITY_EDITOR." >&2
  exit 1
fi

if [[ -z "$TARGET" ]]; then
  echo "Usage: $0 Android|iOS" >&2
  exit 1
fi

case "$TARGET" in
  Android) METHOD="Slime.Editor.MobileBuilder.BuildAndroid" ;;
  iOS) METHOD="Slime.Editor.MobileBuilder.BuildIOS" ;;
  *)
    echo "Unknown target: $TARGET (expected Android or iOS)" >&2
    exit 1
    ;;
esac

mkdir -p "$ROOT/Logs" "$ROOT/Build"
echo "Building $TARGET..."
"$UNITY" \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "$ROOT" \
  -buildTarget "$TARGET" \
  -executeMethod "$METHOD" \
  -logFile "$ROOT/Logs/build-${TARGET}.log"

echo "Done. See Build/ and Logs/build-${TARGET}.log"
