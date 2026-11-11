from pathlib import Path
import re,json
root=Path('D:/MyProject/P_SkyTree'); p=root/'Assets/SkyTree/Prefabs/MainShip_Step01.prefab'
s=p.read_text(); blocks=re.split(r'(?=^--- !u!)',s,flags=re.M)
ids={re.search(r'^--- !u!\d+ &(\d+)',b).group(1):b for b in blocks if b.startswith('--- !u!')}
notes=json.loads((root/'Assets/ArtResources/Ships/MainShip_Step01/TreeOriginalLayers/ImportNotes.json').read_text())
for b in blocks:
 if not b.startswith('--- !u!1 '):continue
 name=re.search(r'^  m_Name: (.*)$',b,re.M).group(1)
 if name not in notes['assets'] and name!='CanopyLowerLeft (1)':continue
 comps=[ids[i] for i in re.findall(r'component: \{fileID: (\d+)\}',b)]
 if name=='CanopyLowerLeft (1)': assert 'm_IsActive: 0' in b;continue
 assert 'm_IsActive: 1' in b
 assert any('guid: '+notes['assets'][name] in c for c in comps)
 assert not any('SkyTree.CanopyRegion' in c for c in comps)
 if name!='TrunkBranches':assert any('SkyTree.CanopySway' in c and 'm_Enabled: 1' in c for c in comps)
 print(name+': direct PNG reference OK; '+('fixed' if name=='TrunkBranches' else 'sway enabled'))
assert len(ids)==len([b for b in blocks if b.startswith('--- !u!')])
