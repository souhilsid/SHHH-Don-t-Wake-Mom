"""Submit a command to the project's existing Unity Editor bridge."""
from pathlib import Path
import sys,json,time,uuid
root=Path(__file__).resolve().parents[1];cmd=sys.argv[1];args=sys.argv[2] if len(sys.argv)>2 else ''
job='v4_'+str(time.time_ns())+'_'+cmd
inbox=root/'Automation/inbox';inbox.mkdir(exist_ok=True,parents=True)
temp=inbox/(job+'.tmp');temp.write_text(json.dumps({'id':job,'cmd':cmd,'args':args}),encoding='utf-8');temp.replace(inbox/(job+'.json'))
out=root/'Automation/outbox'/(job+'.json')
deadline=time.monotonic()+45
while time.monotonic()<deadline:
 if out.exists():
  try:
   result=json.loads(out.read_text(encoding='utf-8-sig'))
   if result.get('status')!='running':print(json.dumps(result,ensure_ascii=True));sys.exit(0 if result.get('status')=='ok' else 1)
  except (ValueError,OSError):pass
 time.sleep(.5)
print(json.dumps({'pending':job,'result':str(out)}))
