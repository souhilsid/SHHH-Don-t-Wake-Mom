#!/bin/bash
# Export the V8 UI kit SVG masters to PNG with Inkscape (cloud workspace or any Inkscape >= 1.2).
set -e; cd "$(dirname "$0")/../DesignSources/UI_V8"
for f in svg/*.svg; do b=$(basename "$f" .svg); w=$(grep -o 'width="[0-9]*"' "$f" | head -1 | grep -o '[0-9]*'); h=$(grep -o 'height="[0-9]*"' "$f" | head -1 | grep -o '[0-9]*');
  inkscape "$f" --export-type=png --export-filename="png/$b.png" -w "$w" -h "$h" >/dev/null 2>&1; done
ls png | wc -l
