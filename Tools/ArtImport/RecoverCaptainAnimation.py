from pathlib import Path
import shutil,datetime
p=Path('Assets/SkyTree/LobbyCaptainWander.cs');s=p.read_text();b=Path('Tools/ArtImport/Backups')/('CaptainAnimation-'+datetime.datetime.now().strftime('%Y%m%d-%H%M%S'));b.mkdir(parents=True);shutil.copy2(p,b/p.name)
s=s.replace('        private void Start() => Initialize();','''        private void OnEnable()
        {
            // Unity restores scene objects after a script reload, but not these runtime lists.
            if (Application.isPlaying) Initialize();
        }

        private void Start() => Initialize();''')
s=s.replace('''            if (actor || !rigPrefab || !ship) return;
            if (staticCaptain) staticCaptain.SetActive(false);''','''            if (!rigPrefab || !ship) return;
            if (actor && animator && sources.Count > 0 && sources.Count == images.Count) return;
            // Rebuild a stale runtime character instead of keeping frozen UI body parts.
            var previous = actor ? actor : ship.Find("Wandering Captain") as RectTransform;
            if (previous)
            {
                previous.gameObject.SetActive(false);
                previous.name = "Retired Captain";
                if (Application.isPlaying) Destroy(previous.gameObject);
                else DestroyImmediate(previous.gameObject);
            }
            sources.Clear();
            images.Clear();
            route.Clear();
            currentStop = visits = 0;
            wait = 1.3f;
            speed = 0;
            if (staticCaptain) staticCaptain.SetActive(false);''')
s=s.replace('''            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;''','''            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            animator.speed = 1f;''')
s=s.replace('''            if (!actor) return;
            if (route.Count == 0)''','''            if (!actor || !animator || sources.Count == 0) Initialize();
            if (!actor || !animator) return;
            if (route.Count == 0)''',1)
s=s.replace('''            if(actor) Destroy(actor.gameObject);''','''            if (actor)
            {
                if (Application.isPlaying) Destroy(actor.gameObject);
                else DestroyImmediate(actor.gameObject);
            }''')
p.write_text(s,encoding='utf-8',newline='\n');print('Added runtime rig recovery on script reload/re-enable; preserved foliage sorting.')
