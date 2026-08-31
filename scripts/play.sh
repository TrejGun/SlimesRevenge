#!/usr/bin/env bash
# Launch Unity Editor Play Mode for Slime's Revenge, wait until ready, then exit.
# Unity keeps running as a normal GUI app. Exit 0 = ready; non-zero = failed.
#
# Usage:
#   ./scripts/play.sh              # Splash → Main menu
#   ./scripts/play.sh --duel       # default duel
#   ./scripts/play.sh --campaign   # default campaign
#
# Env:
#   UNITY_EDITOR         override Unity binary (…/Unity.app/Contents/MacOS/Unity)
#   PLAY_READY_MAX_SEC   max seconds to wait (default 300); polls once per second
#
# Cursor agents: this script must start the editor with `open -na` (LaunchServices),
# never as `Unity &` in the agent tool shell. That shell dies at the end of the turn
# and SIGHUP would take Unity with it — stdout READY is then a lie. See AGENTS.md
# and .cursor/rules/play-mode-unity.mdc.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
UNITY="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity}"
MODE="${1:-}"
MAX_SEC="${PLAY_READY_MAX_SEC:-300}"

METHOD="SlimesRevenge.Editor.MobileBuilder.PlayInEditor"
LOG_NAME="play-splash.log"
READY_RE='\[PlayReady\] Splash'
WRONG_RE='\[PlayReady\] (Duel|Campaign)'

case "$MODE" in
  "")
    METHOD="SlimesRevenge.Editor.MobileBuilder.PlayInEditor"
    LOG_NAME="play-splash.log"
    READY_RE='\[PlayReady\] Splash'
    WRONG_RE='\[PlayReady\] (Duel|Campaign)'
    LABEL="splash"
    ;;
  --duel | duel)
    METHOD="SlimesRevenge.Editor.MobileBuilder.PlayDuel"
    LOG_NAME="play-duel.log"
    READY_RE='\[PlayReady\] Duel'
    WRONG_RE='\[PlayReady\] Campaign'
    LABEL="duel"
    ;;
  --campaign | campaign)
    METHOD="SlimesRevenge.Editor.MobileBuilder.PlayCampaign"
    LOG_NAME="play-campaign.log"
    READY_RE='\[PlayReady\] Campaign'
    WRONG_RE='\[PlayReady\] Duel'
    LABEL="campaign"
    ;;
  -h | --help | help)
    sed -n '2,14p' "$0"
    exit 0
    ;;
  *)
    echo "Unknown mode: $MODE" >&2
    echo "Use: ./scripts/play.sh [|--duel|--campaign]" >&2
    exit 1
    ;;
esac

LOG="$ROOT/Logs/$LOG_NAME"
FAIL_RE='refusing Play Mode|Scripts have compiler errors|error CS[0-9]+|\[Play\] No active camera'

cd "$ROOT"
mkdir -p Logs

# One editor at a time.
"$ROOT/scripts/stop.sh" >/dev/null

rm -f Temp/UnityLockfile "$LOG"
rm -rf Temp/__Backupscenes Assets/_Recovery

# GUI app via LaunchServices — not a child of the caller (Cursor agent) shell.
UNITY_APP="$(cd "$(dirname "$UNITY")/../.." && pwd)"
open -na "$UNITY_APP" --args \
  -projectPath "$ROOT" \
  -executeMethod "$METHOD" \
  -logFile "$LOG"

echo "Starting Unity (${LABEL}) via open -na ${UNITY_APP}…"
echo "Log: ${LOG}"

elapsed=0
while (( elapsed < MAX_SEC )); do
  if [[ -f "$LOG" ]]; then
    if grep -E -q "$FAIL_RE" "$LOG" 2>/dev/null; then
      echo "FAIL: error marker in log (${elapsed}s). See ${LOG}" >&2
      grep -E "$FAIL_RE" "$LOG" | tee /dev/stderr >/dev/null || true
      exit 1
    fi
    if grep -E -q "$WRONG_RE" "$LOG" 2>/dev/null; then
      echo "FAIL: wrong mode ready marker (wanted ${LABEL}). See ${LOG}" >&2
      grep -E "$WRONG_RE" "$LOG" | tee /dev/stderr >/dev/null || true
      exit 1
    fi
    if grep -E -q "$READY_RE" "$LOG" 2>/dev/null; then
      LINE="$(grep -E "$READY_RE" "$LOG" | sed -n '$p')"
      LIVE="$(pgrep -f "Unity.app/Contents/MacOS/Unity -projectPath ${ROOT}" || true)"
      echo "READY: ${LABEL} (${LINE})"
      echo "Unity running (pid ${LIVE}). Use ./scripts/stop.sh to quit."
      exit 0
    fi
  fi

  sleep 1
  elapsed=$((elapsed + 1))
done

echo "FAIL: timed out after ${MAX_SEC}s waiting for ${LABEL}. See ${LOG}" >&2
exit 1
