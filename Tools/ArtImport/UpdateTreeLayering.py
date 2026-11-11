from pathlib import Path
p=Path('Assets/SkyTree/LobbyCaptainWander.cs');s=p.read_text(encoding='utf-8-sig')
a=s.index('            // The painted canopy');b=s.index('\n        }\n\n        private void OnDestroy()',a)
s=s[:a]+'''            // Keep the separate leaf prefab above every ship occupant.
            // Only the captain/cannon order changes with their deck footprints.
            var foliage = ship.Find("SkyTreeFoliage");
            if (foliage && foliage.GetSiblingIndex() != ship.childCount - 1)
                foliage.SetAsLastSibling();
            int targetIndex = ship.childCount - 1;
            if (foliage)
                targetIndex = foliage.GetSiblingIndex() - (actor.GetSiblingIndex() < foliage.GetSiblingIndex() ? 1 : 0);
            var cannon = ship.Find("Cannon") as RectTransform;
            if (cannon && cannon.gameObject.activeInHierarchy)
            {
                Vector3 bottom = cannon.TransformPoint(new Vector3(0, cannon.rect.yMin, 0));
                float cannonFootY = ship.InverseTransformPoint(bottom).y;
                if (actor.anchoredPosition.y > cannonFootY)
                    targetIndex = Mathf.Min(targetIndex, cannon.GetSiblingIndex() - (actor.GetSiblingIndex() < cannon.GetSiblingIndex() ? 1 : 0));
            }
            if (actor.GetSiblingIndex() != targetIndex) actor.SetSiblingIndex(targetIndex);'''+s[b:]
p.write_text(s,encoding='utf-8',newline='\n')
p=Path('Assets/SkyTree/ShipTreeGrowth.cs');s=p.read_text(encoding='utf-8-sig');s=s.replace('[SerializeField] private GameObject[] stages;', '[SerializeField] private GameObject[] stages;\n        [SerializeField] private GameObject[] foliageStages;')
s=s.replace('''            if (stages == null) return;
            for (int i = 0; i < stages.Length; i++)
                if (stages[i]) stages[i].SetActive(i == stage - 1);''','''            ApplyStages(stages);
            ApplyStages(foliageStages);
        }

        private void ApplyStages(GameObject[] targets)
        {
            if (targets == null) return;
            for (int i = 0; i < targets.Length; i++)
                if (targets[i]) targets[i].SetActive(i == stage - 1);''')
p.write_text(s,encoding='utf-8',newline='\n')
# Check sorting for both directions, actor on either side of either sibling.
import itertools
for order in itertools.permutations(['trunk','cannon','foliage','actor']):
 for behind in [False,True]:
  o=list(order);o.remove('foliage');o.append('foliage');a=o.index('actor');f=o.index('foliage');target=f-(a<f)
  if behind: c=o.index('cannon');target=min(target,c-(a<c))
  o.remove('actor');o.insert(target,'actor')
  assert o.index('actor')<o.index('foliage')
  if behind:assert o.index('actor')<o.index('cannon')
print('48 sibling-order cases passed; captain always below foliage.')
