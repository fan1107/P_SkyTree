from pathlib import Path
import re
s=Path('Assets/Prefabs/MainShip/MainShip_Step01_Lobby.prefab').read_text()
bs=re.split(r'(?=^--- !u!)',s,flags=re.M);d={re.match(r'--- !u!\d+ &(\d+)',b)[1]:b for b in bs if b.startswith('--- !u!')}
for key,b in d.items():
 if not b.startswith('--- !u!1 '):continue
 name=re.search(r'^  m_Name: (.*)$',b,re.M)[1]
 if name in ['SkyTree','SkyTreeGreen','Stage01','Stage02','Stage03','MainShip_Step01'] or name.startswith('Canopy'):
  rid=re.findall(r'component: \{fileID: (\d+)\}',b)[0];r=d[rid]
  print(name,key,rid,re.search(r'm_IsActive: (\d)',b)[1]);print('\n'.join(x for x in r.splitlines() if any(v in x for v in ['m_Father','m_AnchoredPosition','m_SizeDelta','m_LocalScale'])))
