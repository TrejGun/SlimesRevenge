#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
UNITY="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity}"
TARGET="${1:-}"

if [[ ! -x "$UNITY" ]]; then
  echo "Unity Editor not found at: $UNITY" >&2
  echo "Install Unity 6000.5.9f1 or set UNITY_EDITOR." >&2
  exit 1
fi

if [[ -z "$TARGET" ]]; then
    echo "Usage: $0 Android|iOS|OSX" >&2
  exit 1
fi

case "$TARGET" in
  Android)
    METHOD="SlimesRevenge.Editor.MobileBuilder.BuildAndroid"
    BUILD_TARGET="Android"
    ;;
  iOS)
    METHOD="SlimesRevenge.Editor.MobileBuilder.BuildIOS"
    BUILD_TARGET="iOS"
    ;;
  OSX|macOS|Mac)
    METHOD="SlimesRevenge.Editor.MobileBuilder.BuildOSX"
    BUILD_TARGET="StandaloneOSX"
    ;;
  *)
    echo "Unknown target: $TARGET (expected Android, iOS, or OSX)" >&2
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
  -buildTarget "$BUILD_TARGET" \
  -executeMethod "$METHOD" \
  -logFile "$ROOT/Logs/build-${TARGET}.log"

echo "Done. See Build/ and Logs/build-${TARGET}.log"
