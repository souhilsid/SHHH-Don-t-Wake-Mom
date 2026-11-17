#!/bin/bash
# Usage: Tools/job.sh <command> [args] [timeoutSeconds]
# Drops a job into the Unity automation inbox and waits for the result (prints the result JSON).
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
CMD="$1"; ARGS="${2:-}"; TO="${3:-170}"
ID="j$(date +%s%N | cut -c1-13)_$CMD"
mkdir -p "$ROOT/Automation/inbox" "$ROOT/Automation/outbox"
ESC=$(printf '%s' "$ARGS" | sed 's/\\/\\\\/g; s/"/\\"/g')
printf '{"id":"%s","cmd":"%s","args":"%s"}' "$ID" "$CMD" "$ESC" > "$ROOT/Automation/inbox/$ID.tmp"
mv "$ROOT/Automation/inbox/$ID.tmp" "$ROOT/Automation/inbox/$ID.json"
OUT="$ROOT/Automation/outbox/$ID.json"
for ((i=0;i<TO;i++)); do
  if [ -f "$OUT" ] && ! grep -q '"status": "running"' "$OUT"; then cat "$OUT"; exit 0; fi
  sleep 1
done
echo "TIMEOUT waiting for $ID (job may still be running; check $OUT)"; [ -f "$OUT" ] && head -c 2000 "$OUT"
exit 3
