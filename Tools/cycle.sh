#!/bin/bash
# Usage: Tools/cycle.sh <sync tgz name or -> [command] [args]
# Applies a sync tarball (in-place), refreshes Unity, waits for the compile, prints errors, optionally starts a command.
cd "$(dirname "$0")/.."
PY=$(command -v python3 || command -v python)
if [ "$1" != "-" ]; then "$PY" Tools/apply_sync.py "_sync/$1" | tail -3; fi
before=$(grep -o '"utc": "[^"]*"' Automation/compile_report.json)
bash Tools/job.sh refresh "" 60 >/dev/null
for i in $(seq 1 40); do sleep 3; now=$(grep -o '"utc": "[^"]*"' Automation/compile_report.json); c=$(grep -o '"compiling": [a-z]*' Automation/heartbeat.json); if [ "$now" != "$before" ] && [ "$c" = '"compiling": false' ]; then break; fi; done
"$PY" - <<'PY'
import json; d=json.load(open('Automation/compile_report.json')); print('compile ok' if d['ok'] else 'COMPILE ERRORS', d['utc']); [print(e) for e in d['errors'][:20]]
PY
sleep 6
if [ -n "$2" ]; then bash Tools/job.sh "$2" "$3" 60 | grep -E '"status"|"message"'; fi
