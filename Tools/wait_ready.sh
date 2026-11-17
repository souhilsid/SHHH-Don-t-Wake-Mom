#!/bin/bash
# Waits until the editor heartbeat is fresh and not compiling/updating. Prints heartbeat + compile report.
ROOT="$(cd "$(dirname "$0")/.." && pwd)"; TO="${1:-170}"
for ((i=0;i<TO;i++)); do
  HB="$ROOT/Automation/heartbeat.json"
  if [ -f "$HB" ]; then
    AGE=$(( $(date +%s) - $(date -r "$HB" +%s) ))
    if [ $AGE -lt 12 ] && grep -q '"compiling": false' "$HB" && grep -q '"updating": false' "$HB"; then
      sleep 2; grep -q '"compiling": false' "$HB" && { cat "$HB"; [ -f "$ROOT/Automation/compile_report.json" ] && head -c 4000 "$ROOT/Automation/compile_report.json"; exit 0; }
    fi
  fi
  sleep 1
done
echo "NOT READY"; [ -f "$ROOT/Automation/heartbeat.json" ] && cat "$ROOT/Automation/heartbeat.json"; exit 3
