from pathlib import Path
import re,uuid,shutil,datetime
root=Path('D:/MyProject/P_SkyTree'); asset=root/'Assets/ArtResources/Cannons/CannonStoutTransparent.png'
prefab=root/'Assets/SkyTree/Prefabs/MainShip_Step01.prefab'
bak=root/'Tools/ArtImport/Backups'/('Cannon-'+datetime.datetime.now().strftime('%Y%m%d-%H%M%S'));bak.mkdir(parents=True,exist_ok=True);shutil.copy2(prefab,bak/prefab.name)
g=uuid.uuid4().hex
meta=(root/'Assets/ArtResources/Ships/MainShip_Step01/HullMidDetail.png.meta').read_text(encoding='utf-8-sig');meta=re.sub(r'^guid: .*$', 'guid: '+g,meta,flags=re.M)
Path(str(asset)+'.meta').write_text(meta,encoding='utf-8')
s=prefab.read_text(encoding='utf-8-sig');blocks=re.split(r'(?=^--- !u!)',s,flags=re.M);n=0
for i,b in enumerate(blocks):
 if b.startswith('--- !u!114 &7999256044126727718\n'):
  assert 'guid: 0fbbb13a6f4443618b06b897d7680905' in b
  blocks[i]=re.sub(r'm_Sprite: \{.*?\}',f'm_Sprite: {{fileID: 21300000, guid: {g}, type: 3}}',b);n+=1
assert n==1
new=''.join(blocks);assert new.replace(g,'0fbbb13a6f4443618b06b897d7680905')==s
prefab.write_text(new,encoding='utf-8',newline='\n')
print('Verified: only Cannon image reference changed; transforms untouched; transparent Sprite Single settings copied.')

