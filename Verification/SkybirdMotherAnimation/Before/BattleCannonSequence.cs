using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkyTree
{
    [DefaultExecutionOrder(100)]
    public sealed class BattleCannonSequence : MonoBehaviour
    {
        [SerializeField] private float shotInterval = 1.35f;
        [SerializeField] private float flightDuration = .72f;
        [SerializeField, Min(1)] private int cannonDamage = 48;
        private RectTransform layer, hitPoint;
        private SkybirdSwarm swarm;
        private RectTransform[] cannons;
        private Vector2[] cannonRest;
        private BattleShipView crewShip;
        private RawImage enemyTemplate;
        private Font enemyFont;
        private int awaitingCannon = -1, pendingCannon = -1;



        private BattleEffectGraphic flash, ball, trail, impact, smoke;
        private BattleEffectGraphic[] sparks;
        private float elapsed, nextShot = .3f, shotAge = 100, impactAge = 100;
        private int nextCannon, activeCannon;
        private Vector2 launch, target, hit;
        public int ShotsFired { get; private set; }
        public int Hits { get; private set; }
        public int LastShotFrame { get; private set; } = -1;
        public int LastShotCannon { get; private set; } = -1;
        public int[] ShotsByCannon { get; private set; }
        public int TimingMismatches { get; private set; }
        public SkybirdSwarm Swarm => swarm;
        public event System.Action EnemiesChanged;
        public bool Finished => crewShip && crewShip.Health != null && (!crewShip.Health.IsAlive || (swarm && swarm.AliveCount == 0));

        public void Build(BattleShipView player, RawImage enemyPrefab, Font font, int enemyCount = 7)
        {
            var playerRect = (RectTransform)player.transform;
            playerRect.localScale = Vector3.one * .82f;
            playerRect.anchoredPosition = new Vector2(-20, -92);
            crewShip = player;
            player.InitializeCrew();
            player.CannonFire += QueueShot;
            enemyTemplate = enemyPrefab; enemyFont = font;
            var guns = new List<RectTransform>();
            foreach (Transform child in player.transform)
                if (child.name.StartsWith("Cannon") && child.gameObject.activeSelf) guns.Add((RectTransform)child);
            cannons = guns.ToArray();
            ShotsByCannon = new int[cannons.Length];
            cannonRest = new Vector2[cannons.Length];
            for (int i = 0; i < cannons.Length; i++) cannonRest[i] = cannons[i].anchoredPosition;
            layer = new GameObject("Cannon fire effects", typeof(RectTransform)).GetComponent<RectTransform>();
            layer.SetParent(transform, false);
            layer.sizeDelta = new Vector2(400,720);
            flash = Effect("Muzzle flash", new Color(1,.78f,.2f,1), 42);
            flash.star = true;
            trail = Effect("Projectile streak", new Color(1,.65f,.12f,.85f), 20);
            ball = Effect("Cannonball glow", new Color(1,.54f,.08f,1), 14);
            var core = Effect("Iron cannonball", new Color(.12f,.10f,.08f,1), 5);
            core.transform.SetParent(ball.transform, false); core.solid = true;
            impact = Effect("Impact burst", new Color(1,.65f,.15f,1), 65);
            impact.star = true;
            smoke = Effect("Impact smoke", new Color(.23f,.22f,.2f,.65f), 40);
            sparks = new BattleEffectGraphic[7];
            for (int i = 0; i < sparks.Length; i++) sparks[i] = Effect("Impact spark " + i, new Color(1,.75f,.2f,1), 7);
            HideEffects();
            StartEncounter(enemyCount, false);
        }

        public void StartEncounter(int count, bool boss)
        {
            if (swarm)
            {
                swarm.EnemyDefeated -= OnEnemyDefeated;
                swarm.Retire();
            }
            awaitingCannon = pendingCannon = -1;
            shotAge = impactAge = 100; nextShot = .3f;
            HideEffects();
            var flock = BattleHudView.Node(boss ? "Boss encounter" : "Skybird Swarm", transform, Vector2.zero, new Vector2(400, 720));
            // Background < birds and their HP < ship < impact effects < HUD.
            flock.SetSiblingIndex(crewShip.transform.GetSiblingIndex());
            swarm = flock.gameObject.AddComponent<SkybirdSwarm>();
            swarm.Build(crewShip, enemyTemplate, enemyFont, count, boss);
            swarm.EnemyDefeated += OnEnemyDefeated;
            hitPoint = swarm.GetTarget(0);
            EnemiesChanged?.Invoke();
        }

        private void OnEnemyDefeated() => EnemiesChanged?.Invoke();

        private BattleEffectGraphic Effect(string name, Color tint, float size)
        {
            var effect = new GameObject(name, typeof(RectTransform)).AddComponent<BattleEffectGraphic>();
            effect.transform.SetParent(layer, false);
            effect.rectTransform.sizeDelta = Vector2.one * size;
            effect.color = tint;
            effect.raycastTarget = false;
            return effect;
        }

        private void HideEffects()
        {
            flash.gameObject.SetActive(false); ball.gameObject.SetActive(false);
            trail.gameObject.SetActive(false); impact.gameObject.SetActive(false); smoke.gameObject.SetActive(false);
            foreach (var spark in sparks) spark.gameObject.SetActive(false);
        }

        private Vector2 InLayer(Vector3 world) => layer.InverseTransformPoint(world);

        private void QueueShot(int cannon)
        {
            if (!isActiveAndEnabled || Finished || cannon != awaitingCannon) return;
            pendingCannon = cannon; awaitingCannon = -1;
        }
        private void OnDestroy()
        {
            if (crewShip) crewShip.CannonFire -= QueueShot;
            if (swarm) swarm.EnemyDefeated -= OnEnemyDefeated;
        }

        private void LateUpdate()
        {
            if (!swarm || cannons == null || cannons.Length == 0) return;
            if (Finished)
            {
                awaitingCannon = pendingCannon = -1;
                HideEffects();
                for (int i = 0; i < cannons.Length; i++) cannons[i].anchoredPosition = cannonRest[i];
                return;
            }
            float dt = Time.deltaTime;
            elapsed += dt;
            impactAge += dt;
            // Keep the projectile's target locked until this shot has landed.
            if (shotAge >= flightDuration || !swarm.IsAlive(hitPoint)) hitPoint = swarm.GetTarget(ShotsFired);
            if (!hitPoint) { HideEffects(); return; }
            bool fired = pendingCannon >= 0;
            if (fired) { activeCannon = pendingCannon; pendingCannon = -1; shotAge = 0; }
            for (int i=0;i<cannons.Length;i++)
            {
                var gun = cannons[i];
                Vector2 localTarget = gun.parent.InverseTransformPoint(hitPoint.position);
                Vector2 direction = (localTarget-cannonRest[i]).normalized;
                // Keep the carriage upright and within reach of the gunner's hands.
                float aim = Mathf.DeltaAngle(45f, Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
                gun.localRotation = Quaternion.Euler(0,0,Mathf.Clamp(aim,-12f,12f));
                float recoil = i==activeCannon ? Mathf.Sin(Mathf.Clamp01(shotAge/.28f)*Mathf.PI)*3f : 0;
                gun.anchoredPosition = cannonRest[i]-direction*recoil;
            }
            nextShot -= dt;
            if (nextShot<=0 && awaitingCannon < 0)
            {
                int index = nextCannon % cannons.Length;
                if (crewShip.BeginCannonAttack(index))
                {
                    awaitingCannon = index; nextCannon++;
                    nextShot = Mathf.Max(shotInterval, 1.05f);
                }
            }
            if (fired)
            {
                var gun = cannons[activeCannon];
                launch = InLayer(gun.TransformPoint(new Vector3(gun.rect.width*.36f,gun.rect.height*.36f,0)));
                target = InLayer(hitPoint.position);
                ShotsFired++; ShotsByCannon[activeCannon]++;
                LastShotFrame = Time.frameCount; LastShotCannon = activeCannon;
                if (crewShip.Crew[activeCannon].LastFireFrame != LastShotFrame) TimingMismatches++;
            }
            float previous = shotAge;
            if (!fired) shotAge += dt;
            flash.gameObject.SetActive(shotAge<.15f);
            flash.rectTransform.anchoredPosition = InLayer(cannons[activeCannon].TransformPoint(new Vector3(cannons[activeCannon].rect.width*.36f,cannons[activeCannon].rect.height*.36f,0)));
            flash.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.3f,.25f,Mathf.Clamp01(shotAge/.15f));
            bool flying = shotAge<flightDuration;
            ball.gameObject.SetActive(flying); trail.gameObject.SetActive(flying);
            if (flying)
            {
                target = InLayer(hitPoint.position);
                float t = shotAge/flightDuration;
                Vector2 p = Vector2.Lerp(launch,target,t);
                ball.rectTransform.anchoredPosition = p;
                Vector2 d = (target-launch).normalized;
                trail.rectTransform.anchoredPosition = p-d*8;
                trail.rectTransform.sizeDelta = new Vector2(25,5);
                trail.rectTransform.localRotation = Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);
            }
            if (previous<flightDuration && shotAge>=flightDuration)
            {
                hit = InLayer(hitPoint.position); impactAge=0;
                if (swarm.Hit(hitPoint, cannonDamage)) Hits++;
            }
            impact.gameObject.SetActive(impactAge<.3f);
            impact.rectTransform.anchoredPosition = hit;
            impact.rectTransform.localScale = Vector3.one * Mathf.Lerp(.3f,1.2f,Mathf.Clamp01(impactAge/.3f));
            impact.color = new Color(1,.65f,.15f,Mathf.Clamp01(1-impactAge/.3f));
            smoke.gameObject.SetActive(impactAge<1.1f);
            smoke.rectTransform.anchoredPosition = hit+new Vector2(impactAge*8,impactAge*18);
            smoke.rectTransform.localScale = Vector3.one * (1+Mathf.Min(impactAge,1));
            smoke.color = new Color(.22f,.20f,.18f,.65f*Mathf.Clamp01(1-impactAge/1.1f));
            for(int i=0;i<sparks.Length;i++)
            {
                sparks[i].gameObject.SetActive(impactAge<.5f);
                float angle=i*Mathf.PI*2/sparks.Length;
                sparks[i].rectTransform.anchoredPosition = hit+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*impactAge*65;
                sparks[i].color = new Color(1,.75f,.2f,Mathf.Clamp01(1-impactAge/.5f));
            }
        }
    }
}
