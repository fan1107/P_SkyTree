import json, urllib.request, uuid
from pathlib import Path
base='http://127.0.0.1:8188'
out=Path('Tools/ArtImport/ComfyCannon')
source=Path('C:/Users/fan/AppData/Local/Temp/codex-clipboard-97ca6de7-8c10-4096-84a8-830554d37af3.png')
boundary='----'+uuid.uuid4().hex
body=('--'+boundary+'\r\nContent-Disposition: form-data; name="image"; filename="skytree_cannon_source.png"\r\nContent-Type: image/png\r\n\r\n').encode()+source.read_bytes()+('\r\n--'+boundary+'--\r\n').encode()
r=json.load(urllib.request.urlopen(urllib.request.Request(base+'/upload/image',body,{'Content-Type':'multipart/form-data; boundary='+boundary})))
w=json.loads(Path('D:/ComfyUI/krea2-automation/workflow_ship_acidic_preserve_lora08_api.json').read_text(encoding='utf-8-sig'))
w['20']['inputs']['image']=r['name']
w['5']['inputs']['width']=1024;w['5']['inputs']['height']=816
w['6']['inputs']['text']='Isometric fantasy mobile game cannon asset, preserve the exact original cannon geometry, camera angle and orientation, barrel pointing toward upper right, four-wheel wooden gun carriage, ropes and iron fasteners. Hand painted stylized game illustration matching an aged wooden fantasy ship, warm muted honey ochre and russet wood, dark umber shadows, worn gray iron cannon barrel with restrained antique brass accents, readable dark outlines, soft bevel highlights, simplified painterly surface detail, matte materials, subtle weathering, coherent plank grain. Single cannon centered on plain light gray background, no text, no people, no extra objects. Preserve silhouette and all mechanical parts.'
w['29']['inputs']['filename_prefix']='SkyTree/CannonMatched'
(out/'workflow_api.json').write_text(json.dumps(w,indent=2),encoding='utf-8')
payload=json.dumps({'prompt':w,'client_id':'skytree-cannon-'+uuid.uuid4().hex}).encode()
result=json.load(urllib.request.urlopen(urllib.request.Request(base+'/prompt',payload,{'Content-Type':'application/json'})))
(out/'job.json').write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps(result))
