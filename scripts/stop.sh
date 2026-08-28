#!/usr/bin/env bash
# Gracefully stop the Unity Editor instance for this project.
# Finds the process by -projectPath (no PID file, no log scraping).
# Exit 0 = no editor left for this project; non-zero = still running.
#
# Usage:
#   ./scripts/stop.sh
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"

unity_pids() {
  pgrep -f "Unity.app/Contents/MacOS/Unity -projectPath ${ROOT}" || true
}

PIDS="$(unity_pids)"

if [[ -z "${PIDS}" ]]; then
  echo "STOPPED: no Unity Editor for this project"
  exit 0
fi

echo "Stopping Unity Editor (pid ${PIDS})…"
# shellcheck disable=SC2086
kill -TERM ${PIDS} 2>/dev/null || true
sleep 1

LEFT="$(unity_pids)"
if [[ -n "${LEFT}" ]]; then
  echo "Still running — force kill (pid ${LEFT})…"
  # shellcheck disable=SC2086
  kill -KILL ${LEFT} 2>/dev/null || true
  sleep 1
fi

rm -f "${ROOT}/Temp/UnityLockfile"

LEFT="$(unity_pids)"
if [[ -n "${LEFT}" ]]; then
  echo "FAIL: Unity still running (pid ${LEFT})" >&2
  exit 1
fi

echo "STOPPED: Unity Editor quit"
exit 0
