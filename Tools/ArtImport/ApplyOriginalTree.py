from pathlib import Path
import re, uuid, json, shutil, datetime
root=Path('D:/MyProject/P_SkyTree')
prefab=root/'Assets/SkyTree/Prefabs/MainShip_Step01.prefab'
out=root/'Assets/ArtResources/Ships/MainShip_Step01/TreeOriginalLayers'
backup=root/'Tools/ArtImport/Backups'/datetime.datetime.now().strftime('%Y%m%d-%H%M%S')
backup.mkdir(parents=True,exist_ok=True);shutil.copy2(prefab,backup/prefab.name)
meta=(root/'Assets/ArtResources/Ships/MainShip_Step01/CanopyTop.png.meta').read_text(encoding='utf-8-sig')
guids={}
for name in ('TrunkBranches','CanopyTop','CanopyLowerLeft','CanopyRight'):
    g=uuid.uuid4().hex;guids[name]=g
    m=re.sub(r'^guid: .*$', 'guid: '+g,meta,flags=re.M)
    (out/(name+'.png.meta')).write_text(m,encoding='utf-8')
s=prefab.read_text(encoding='utf-8-sig')
blocks=re.split(r'(?=^--- !u!)',s,flags=re.M)
byid={re.search(r'^--- !u!\d+ &(\d+)',b).group(1):i for i,b in enumerate(blocks) if b.startswith('--- !u!')}
images='fe87c0e1cc204ed48ad3b37840f39efc'
w=265*933/1109
pivots={'TrunkBranches':(.5,.5),'CanopyTop':(.43,.56),'CanopyLowerLeft':(.36,.35),'CanopyRight':(.57,.45)}
changed=[]
for i,b in enumerate(list(blocks)):
    if not b.startswith('--- !u!1 '):continue
    match=re.search(r'^  m_Name: (.*)$',b,re.M)
    if not match:continue
    name=match.group(1)
    if name=='CanopyLowerLeft (1)':
        blocks[i]=re.sub(r'm_IsActive: 1', 'm_IsActive: 0', b);continue
    if name not in guids:continue
    ids=re.findall(r'component: \{fileID: (\d+)\}',b)
    for cid in ids:
        j=byid[cid];comp=blocks[j]
        if comp.startswith('--- !u!224 '):
            px,py=pivots[name]
            replacements={'m_AnchorMin':'{x: 0.5, y: 0.5}','m_AnchorMax':'{x: 0.5, y: 0.5}',
              'm_SizeDelta':f'{{x: {w:.7f}, y: 265}}','m_Pivot':f'{{x: {px}, y: {py}}}',
              'm_AnchoredPosition':f'{{x: {(px-.5)*w:.7f}, y: {(py-.5)*265:.7f}}}'}
            for key,val in replacements.items():comp=re.sub(r'^  '+key+r': .*$', '  '+key+': '+val,comp,flags=re.M)
        elif images in comp:
            comp=re.sub(r'm_Sprite: \{.*?\}',f'm_Sprite: {{fileID: 21300000, guid: {guids[name]}, type: 3}}',comp)
            comp=comp.replace('m_PreserveAspect: 1','m_PreserveAspect: 0').replace('m_UseSpriteMesh: 1','m_UseSpriteMesh: 0')
        blocks[j]=comp
    changed.append(name)
assert sorted(changed)==sorted(guids)
prefab.write_text(''.join(blocks),encoding='utf-8',newline='\n')
info={'source':'codex-clipboard-64fea0fb-30c7-474c-8502-6796b5adbaf8.png','method':'Original RGB pixels retained; neutral dark background alpha removed; visible wood and 3 foliage groups exported as separate PNGs. No repainting or hidden branch reconstruction.','assets':guids,'prefab':str(prefab),'backup':str(backup),'animation':'Existing CanopySway on 3 active leaves; extra old leaf duplicate retained inactive.'}
(out/'ImportNotes.json').write_text(json.dumps(info,ensure_ascii=False,indent=2),encoding='utf-8')
for name,g in guids.items():assert f'guid: {g}' in prefab.read_text()
print(json.dumps(info,ensure_ascii=False,indent=2))

