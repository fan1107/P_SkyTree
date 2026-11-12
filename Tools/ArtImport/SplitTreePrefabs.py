from pathlib import Path
import re,uuid,shutil,datetime
root=Path('D:/MyProject/P_SkyTree');folder=root/'Assets/Prefabs/Trees';p=root/'Assets/Prefabs/MainShip/MainShip_Step01_Lobby.prefab';s=p.read_text(encoding='utf-8-sig')
backup=root/'Tools/ArtImport/Backups'/('SplitTree-'+datetime.datetime.now().strftime('%Y%m%d-%H%M%S'));backup.mkdir(parents=True)
for f in [p,root/'Assets/SkyTree/LobbyCaptainWander.cs',root/'Assets/SkyTree/ShipTreeGrowth.cs']:shutil.copy2(f,backup/f.name)
blocks=re.split(r'(?=^--- !u!)',s,flags=re.M);header=blocks[0]
d={re.match(r'--- !u!\d+ &(\d+)',b)[1]:b for b in blocks[1:]}
gos={};rectGo={}
for fid,b in d.items():
 if b.startswith('--- !u!1 '):
  name=re.search(r'^  m_Name: (.*)$',b,re.M)[1];comps=re.findall(r'component: \{fileID: (\d+)\}',b);gos[fid]=(name,comps);rectGo[comps[0]]=fid
rootGo=next(i for i,(n,c) in gos.items() if n=='SkyTree');rootRect=gos[rootGo][1][0];shipRect='6816401477851896264'
def children(r):
 b=d[r];section=b.split('  m_Children:')[1].split('  m_Father:')[0];return re.findall(r'fileID: (\d+)',section)
def subtree(r):
 result=[rectGo[r]]
 for c in children(r):result+=subtree(c)
 return result
allgo=subtree(rootRect);allids=set(allgo)
for g in allgo:allids.update(gos[g][1])
stages=[next(g for g in allgo if gos[g][0]=='Stage0'+str(i)) for i in range(1,4)]
def isleaf(n):return n.startswith('Canopy') or n.startswith('Leaves') or n=='Foliage'
keep={}
guids={}
for role in ['Trunk','Foliage']:
 kept=[]
 for g in allgo:
  n,cs=gos[g];container=bool(children(cs[0]))
  if container or (isleaf(n)==(role=='Foliage')):kept.append(g)
 ids=set(kept)
 for g in kept:ids.update(gos[g][1])
 # The grouping node contains a disabled legacy whole-tree Image; omit it in both new prefabs.
 remove=set()
 for g in kept:
  if gos[g][0]=='SkyTreeGreen':remove.update(gos[g][1][1:])
 ids-=remove
 out=[]
 for fid,b in d.items():
  if fid not in ids:continue
  if fid==rootGo:b=b.replace('m_Name: SkyTree\n','m_Name: SkyTree'+role+'\n')
  if fid==rootRect:b=b.replace('m_Father: {fileID: '+shipRect+'}','m_Father: {fileID: 0}')
  for rid in remove:b=re.sub(r'^  - component: \{fileID: '+rid+r'\}\n','',b,flags=re.M)
  if b.startswith('--- !u!224 '):
   old=children(fid);new=[c for c in old if c in ids]
   val='\n'+''.join('  - {fileID: '+c+'}\n' for c in new) if new else ' []\n'
   b=re.sub(r'  m_Children:.*?(?=  m_Father:)',lambda m:'  m_Children:'+val,b,flags=re.S)
  out.append(b)
 fp=folder/('SkyTree'+role+'.prefab');assert not fp.exists(),fp
 fp.write_text(header+''.join(out),encoding='utf-8');g=uuid.uuid4().hex;guids[role]=g
 Path(str(fp)+'.meta').write_text('fileFormatVersion: 2\nguid: '+g+'\nPrefabImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')
 keep[role]=ids
# Nested prefab instances preserve the original trunk root and stage fileIDs for existing references.
used=set(d)
def newid():
 while True:
  n=str(uuid.uuid4().int%(2**62)+1)
  if n not in used:used.add(n);return n
leafRoot=newid();leafStages=[newid() for _ in stages]
append=[]
for role in ['Trunk','Foliage']:
 instance=newid();guid=guids[role]
 append.append(f'''--- !u!1001 &{instance}
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {{fileID: {shipRect}}}
    m_Modifications: []
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {{fileID: 100100000, guid: {guid}, type: 3}}
''')
 exposed=[(rootRect,rootRect if role=='Trunk' else leafRoot,'224','RectTransform')]+[(g,g if role=='Trunk' else leafStages[i],'1','GameObject') for i,g in enumerate(stages)]
 for source,dest,typ,label in exposed:
  append.append(f'''--- !u!{typ} &{dest} stripped
{label}:
  m_CorrespondingSourceObject: {{fileID: {source}, guid: {guid}, type: 3}}
  m_PrefabInstance: {{fileID: {instance}}}
  m_PrefabAsset: {{fileID: 0}}
''')
out=[]
for fid,b in d.items():
 if fid in allids:continue
 if fid==shipRect:
  b=b.replace('  m_Father:', '  - {fileID: '+leafRoot+'}\n  m_Father:',1)
 if 'Assembly-CSharp::SkyTree.ShipTreeGrowth' in b:
  b+='  foliageStages:\n'+''.join('  - {fileID: '+g+'}\n' for g in leafStages)
 out.append(b)
p.write_text(header+''.join(out+append),encoding='utf-8')
# Ensure no original-tree local reference became dangling.
new=p.read_text();localids=set(re.findall(r'^--- !u!\d+ &(\d+)',new,re.M))
for fid in re.findall(r'\{fileID: (\d+)\}',new):assert fid=='0' or fid in localids,('Dangling reference',fid)
print('Created SkyTreeTrunk.prefab and SkyTreeFoliage.prefab; all local references valid.')
print('Preserved leaf objects:',[gos[g][0] for g in allgo if isleaf(gos[g][0])])
print('Backup:',backup)
