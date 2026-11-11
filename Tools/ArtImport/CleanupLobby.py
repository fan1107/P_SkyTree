from pathlib import Path
import re,shutil,datetime
root=Path('D:/MyProject/P_SkyTree'); folder=root/'Assets/SkyTree/Prefabs'
backup=root/'Tools/ArtImport/Backups'/('LobbyCleanup-'+datetime.datetime.now().strftime('%Y%m%d-%H%M%S'));backup.mkdir(parents=True)
for name,duplicate in [('MainShip_Step01.prefab','Cannon (1)'),('LobbyView.prefab','Cannon (2)')]:
 p=folder/name;shutil.copy2(p,backup/name);s=p.read_text(encoding='utf-8-sig');blocks=re.split(r'(?=^--- !u!)',s,flags=re.M)
 doomed=set();rects=[]
 for b in blocks:
  if b.startswith('--- !u!1 ') and f'  m_Name: {duplicate}\n' in b:
   doomed.add(re.search(r'&(\d+)',b).group(1));ids=re.findall(r'component: \{fileID: (\d+)\}',b);doomed.update(ids);rects.append(ids[0])
 assert len(rects)==1,(name,rects)
 kept=[]
 for b in blocks:
  m=re.match(r'--- !u!\d+ &(\d+)',b)
  if m and m.group(1) in doomed:continue
  for rid in rects:
   b=re.sub(r'^  - \{fileID: '+rid+r'\}\n','',b,flags=re.M)
   b=re.sub(r'    - targetCorrespondingSourceObject: [^\n]+\n      insertIndex: [^\n]+\n      addedObject: \{fileID: '+rid+r'\}\n','',b)
  b=b.replace('    m_AddedGameObjects:\n    m_AddedComponents:', '    m_AddedGameObjects: []\n    m_AddedComponents:')
  kept.append(b)
 new=''.join(kept)
 for i in doomed:assert not re.search(r'fileID: '+i+r'\b',new),(name,i)
 if name=='MainShip_Step01.prefab':
  guid=re.search(r'^guid: (.+)$',(root/'Assets/ArtResources/Cannons/CannonStoutTransparent.png.meta').read_text(encoding='utf-8-sig'),re.M).group(1)
  new=re.sub(r'(?ms)(^--- !u!114 &7999256044126727718\n.*?m_Sprite: )\{[^}]+\}',lambda m:m.group(1)+'{fileID: 21300000, guid: '+guid+', type: 3}',new)
 p.write_text(new,encoding='utf-8',newline='\n');print('Removed '+duplicate+' from '+name)
p=root/'Assets/SkyTree/LobbyCaptainWander.cs';shutil.copy2(p,backup/p.name);s=p.read_text(encoding='utf-8-sig')
a=s.index('            if(tree)\n');b=s.index('\n        }',a)
s=s[:a]+'''            // The painted canopy has no per-branch depth. Keep the captain readable
            // in front of it rather than switching the entire tree over the actor.
            // The cannon still occludes a captain standing behind its deck footprint.
            var cannon = ship.Find("Cannon") as RectTransform;
            actor.SetAsLastSibling();
            if (cannon && cannon.gameObject.activeInHierarchy)
            {
                Vector3 bottom = cannon.TransformPoint(new Vector3(0, cannon.rect.yMin, 0));
                float cannonFootY = ship.InverseTransformPoint(bottom).y;
                if (actor.anchoredPosition.y > cannonFootY)
                    actor.SetSiblingIndex(cannon.GetSiblingIndex());
            }'''+s[b:]
# Cache the current cannon footprint for routing, instead of an obsolete fixed location.
s=s.replace('            if (staticCaptain) staticCaptain.SetActive(false);','''            if (staticCaptain) staticCaptain.SetActive(false);
            var cannon = ship.Find("Cannon") as RectTransform;
            if (cannon)
            {
                Vector3 bottom = cannon.TransformPoint(new Vector3(0, cannon.rect.yMin, 0));
                cannonBase = ship.InverseTransformPoint(bottom);
            }''')
p.write_text(s,encoding='utf-8',newline='\n')
print('Updated captain occlusion and cannon obstacle position. Backup: '+str(backup))
