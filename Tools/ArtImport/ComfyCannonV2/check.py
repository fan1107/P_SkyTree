import json,time,urllib.request
from pathlib import Path
out=Path('Tools/ArtImport/ComfyCannonV2'); job=json.loads((out/'job.json').read_text(encoding='utf-8-sig'))['prompt_id']
for i in range(3):
 h=json.load(urllib.request.urlopen('http://127.0.0.1:8188/history/'+job,timeout=15))
 if job in h:
  (out/'history.json').write_text(json.dumps(h[job],indent=2),encoding='utf-8');print(json.dumps({'status':h[job]['status'],'outputs':h[job]['outputs']}));break
 if i<2:time.sleep(20)
else:print('Still running')
