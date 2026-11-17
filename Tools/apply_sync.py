#!/usr/bin/env python3
"""Extract a sync tarball in place WITHOUT unlinking files (connected folder forbids deletes): rewrites file contents.
Retries files that are briefly locked on the Windows side and never stops half-way silently."""
import sys, tarfile, os, time
root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
failed = []
with tarfile.open(sys.argv[1]) as t:
    for m in t.getmembers():
        if not m.isfile(): continue
        dst = os.path.join(root, m.name)
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        data = t.extractfile(m).read()
        for attempt in range(6):
            try:
                with open(dst, 'wb') as f: f.write(data)
                print('wrote', m.name, len(data)); break
            except PermissionError as e:
                time.sleep(1.5)
        else:
            failed.append(m.name)
if failed: print('FAILED:', failed); sys.exit(1)
