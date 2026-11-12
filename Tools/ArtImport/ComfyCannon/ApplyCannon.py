from pathlib import Path
import re,uuid,shutil,datetime
root=Path('D:/MyProject/P_SkyTree'); asset=root/'Assets/ArtResources/Cannons/CannonComfyTransparent.png'
prefab=root/'Assets/Prefabs/MainShip/MainShip_Step01_Lobby.prefab'
bak=root/'Tools/ArtImport/Backups'/('Cannon-'+datetime.datetime.now().strftime('%Y%m%d-%H%M%S'));bak.mkdir(parents=True,exist_ok=True);shutil.copy2(prefab,bak/prefab.name)
g=uuid.uuid4().hex
meta=(root/'Assets/ArtResources/Ships/MainShip_Step01/HullMidDetail.png.meta').read_text(encoding='utf-8-sig');meta=re.sub(r'^guid: .*$', 'guid: '+g,meta,flags=re.M)
Path(str(asset)+'.meta').write_text(meta,encoding='utf-8')
s=prefab.read_text(encoding='utf-8-sig');blocks=re.split(r'(?=^--- !u!)',s,flags=re.M);n=0
for i,b in enumerate(blocks):
 if b.startswith('--- !u!114 &7999256044126727718\n'):
  assert 'guid: 8cf7de3d29728374ca4192c47b5768d5' in b
  blocks[i]=re.sub(r'm_Sprite: \{.*?\}',f'm_Sprite: {{fileID: 21300000, guid: {g}, type: 3}}',b);n+=1
assert n==1
new=''.join(blocks);assert new.replace(g,'8cf7de3d29728374ca4192c47b5768d5')==s
prefab.write_text(new,encoding='utf-8',newline='\n')
print('Verified: only Cannon image reference changed; transforms untouched; transparent Sprite Single settings copied.')
