using UnityEngine;
using UnityEngine.UI;

namespace SkyTree
{
    // Birds strike the upper hull rim, then retreat. Dead birds never attack again.
    public sealed class SkybirdSwarm : MonoBehaviour
    {
        [SerializeField, Min(1)] private int enemyHealth = 144;
        [SerializeField, Min(1)] private int collisionDamage = 18;
        [SerializeField, Min(1)] private int bossHealth = 720;
        [SerializeField, Min(1)] private int bossCollisionDamage = 36;
        private sealed class Bird
        {
            public RectTransform rect;
            public RawImage image;
            public SkybirdAnimation animation;
            public BattleEffectGraphic burst;
            public BattleHealth health;
            public BattleHealthBar bar;
            public Text damageText;
            public Text nameLabel;
            public Vector2 damageOrigin, contact;
            public float damageAge = 10, deathAge, approach;
            public Vector2 home, from, strike;
            public int state;
            public float age, wait, flash;
        }
        private Bird[] birds = new Bird[0];
        private BattleShipView player;
        private RectTransform space;
        private RectTransform effects;
        private float elapsed;
        public int CollisionCount { get; private set; }
        public int RepelledCount { get; private set; }
        public int Count => birds.Length;
        public int AliveCount { get; private set; }
        public bool IsBoss { get; private set; }
        public event System.Action EnemyDefeated;

        public void Build(BattleShipView ship, RawImage prefab, Font font, int count = 7, bool boss = false)
        {
            player = ship; space = (RectTransform)transform;
            IsBoss = boss;
            birds = new Bird[boss ? 1 : Mathf.Clamp(count, 1, 7)];
            AliveCount = birds.Length;
            effects = BattleHudView.Node("Enemy impacts and damage", transform.parent, Vector2.zero, new Vector2(400, 720));
            effects.SetSiblingIndex(ship.transform.GetSiblingIndex() + 1);
            Vector2[] homes = {new Vector2(-135,205),new Vector2(-65,240),new Vector2(5,200),
                new Vector2(80,235),new Vector2(140,175),new Vector2(-100,140),new Vector2(60,140)};
            if (count == 3) homes = new[] { new Vector2(-110, 205), new Vector2(0, 228), new Vector2(110, 205) };
            if (boss) homes = new[] { new Vector2(0, 188) };
            for(int i=0;i<birds.Length;i++)
            {
                // Reserve space between the battle heading and the overhead HP bars.
                homes[i] -= Vector2.up * 18;
                var image = Instantiate(prefab, space, false);
                image.name = boss ? "Skybird Boss" : "Skybird " + (i+1);
                image.raycastTarget = false;
                image.rectTransform.sizeDelta *= boss ? 1.85f : 1f + (i%3) * (3f/46f);
                image.rectTransform.anchoredPosition=homes[i];
                var animation = image.GetComponent<SkybirdAnimation>();
                if (animation) animation.PlayIdle(i * .17f);
                var burst = new GameObject("Collision flash "+(i+1),typeof(RectTransform)).AddComponent<BattleEffectGraphic>();
                burst.transform.SetParent(effects,false); burst.rectTransform.sizeDelta=Vector2.one*25;
                burst.star=true; burst.raycastTarget=false; burst.gameObject.SetActive(false);
                var health = new BattleHealth(Mathf.Max(1, boss ? bossHealth : enemyHealth));
                var bar = BattleHudView.Node("Skybird HP " + (i+1), space, Vector2.zero, boss ? new Vector2(100, 10) : new Vector2(43, 6.5f)).gameObject.AddComponent<BattleHealthBar>();
                bar.Bind(health, false);
                var damageText = BattleHudView.Label("Skybird damage " + (i+1), effects, font, "", Vector2.zero, new Vector2(64, 25), 15);
                var outline = damageText.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color32(40, 28, 15, 230); outline.effectDistance = new Vector2(1, -1);
                damageText.gameObject.SetActive(false);
                birds[i]=new Bird {rect=image.rectTransform,image=image,animation=animation,home=homes[i],wait=1.4f+i*.62f,burst=burst,
                    health=health,bar=bar,damageText=damageText,approach=boss ? .75f : Mathf.InverseLerp(-145,145,homes[i].x)};
                if (boss) birds[i].nameLabel = BattleHudView.Label("Boss name", space, font, "구름새 우두머리", Vector2.zero, new Vector2(130, 18), 11);
                PositionBar(birds[i]);
            }
        }

        public void Retire()
        {
            gameObject.SetActive(false);
            if (effects) effects.gameObject.SetActive(false);
            Destroy(gameObject);
        }

        private void OnDestroy() { if (effects) Destroy(effects.gameObject); }

        public RectTransform GetTarget(int index)
        {
            if (birds.Length == 0) return null;
            int start = (index % birds.Length + birds.Length) % birds.Length;
            for (int offset = 0; offset < birds.Length; offset++)
            {
                var bird = birds[(start + offset) % birds.Length];
                if (bird != null && bird.health.IsAlive) return bird.rect;
            }
            return null;
        }

        public BattleHealth GetHealth(RectTransform target)
        {
            foreach (var bird in birds) if (bird != null && bird.rect == target) return bird.health;
            return null;
        }

        public bool IsAlive(RectTransform target) => target && GetHealth(target)?.IsAlive == true;

        public bool Hit(RectTransform target, int damage = 48)
        {
            foreach(var bird in birds)
                if(bird != null && bird.rect==target)
                {
                    int applied = bird.health.Damage(damage);
                    if (applied == 0) return false;
                    bird.damageText.text = "-" + applied;
                    bird.damageOrigin = bird.rect.anchoredPosition + new Vector2(24, 14);
                    bird.damageText.rectTransform.anchoredPosition = bird.damageOrigin;
                    bird.damageText.color = new Color(1, .94f, .8f, 1);
                    bird.damageAge = 0;
                    bird.damageText.gameObject.SetActive(true);
                    bird.from=bird.rect.anchoredPosition; bird.state=2; bird.age=0; bird.flash=.3f;
                    if (bird.animation) bird.animation.SetAttacking(false);
                    bird.burst.rectTransform.anchoredPosition=bird.from;
                    RepelledCount++;
                    if (!bird.health.IsAlive)
                    {
                        if (bird.animation) bird.animation.StopAnimation();
                        AliveCount--;
                        bird.bar.gameObject.SetActive(false);
                        if (bird.nameLabel) bird.nameLabel.gameObject.SetActive(false);
                        bird.burst.gameObject.SetActive(false);
                        EnemyDefeated?.Invoke();
                    }
                    return true;
                }
            return false;
        }

        private void PositionBar(Bird bird)
        {
            var position = bird.rect.anchoredPosition + Vector2.up * (bird.rect.rect.height * .55f + 6);
            position.x = Mathf.Clamp(position.x, -175, 175);
            bird.bar.rectTransform.anchoredPosition = position;
            if (bird.nameLabel) bird.nameLabel.rectTransform.anchoredPosition = position + Vector2.up * 15;
        }

        private void Update()
        {
            if(!player) return;
            float dt=Time.deltaTime; elapsed+=dt;
            for(int i=0;i<birds.Length;i++)
            {
                var b=birds[i];
                b.damageAge += dt;
                b.damageText.gameObject.SetActive(b.damageAge < .8f);
                if (b.damageAge < .8f)
                {
                    b.damageText.rectTransform.anchoredPosition = b.damageOrigin + Vector2.up * b.damageAge * 25;
                    b.damageText.color = new Color(1, .94f, .8f, Mathf.Clamp01((.8f - b.damageAge) / .35f));
                }
                if (!b.health.IsAlive)
                {
                    b.deathAge += dt;
                    b.image.color = new Color(1, .75f, .6f, Mathf.Clamp01(1 - b.deathAge / .4f));
                    b.rect.anchoredPosition = b.from + Vector2.up * b.deathAge * 18;
                    b.rect.localScale = Vector3.one * Mathf.Lerp(1, .75f, b.deathAge / .4f);
                    if (b.deathAge >= .4f) b.image.gameObject.SetActive(false);
                    continue;
                }
                if (!player.Health.IsAlive)
                {
                    if (b.animation) b.animation.SetAttacking(false);
                    b.burst.gameObject.SetActive(false);
                    continue;
                }
                b.age+=dt; b.flash=Mathf.Max(0,b.flash-dt);
                b.image.color=Color.Lerp(IsBoss ? new Color(1,.78f,.58f,1) : Color.white,new Color(1,.45f,.3f,1),b.flash/.3f);
                Vector2 hover=b.home+new Vector2(Mathf.Sin(elapsed*1.3f+i)*9,Mathf.Sin(elapsed*2+i*1.7f)*5);
                if(b.state==0)
                {
                    b.rect.anchoredPosition=hover;
                    if(b.age>=b.wait)
                    {
                        b.state=1;b.age=0;b.from=b.rect.anchoredPosition;
                    }
                }
                else if(b.state==1)
                {
                    float t=Mathf.Clamp01(b.age/.9f);
                    // Follow the bobbing upper rim, stopping the bird's body above it.
                    b.contact=space.InverseTransformPoint(player.GetUpperHullContact(b.approach));
                    b.strike=b.contact+Vector2.up*(b.rect.rect.height*.32f);
                    b.rect.anchoredPosition=Vector2.Lerp(b.from,b.strike,t*t);
                    if(t>=1)
                    {
                        if (player.TakeDamage(IsBoss ? bossCollisionDamage : collisionDamage) > 0) CollisionCount++;
                        b.burst.rectTransform.anchoredPosition=b.contact;
                        b.flash=.25f; b.state=2;b.age=0;b.from=b.strike;
                    }
                }
                else
                {
                    float t=Mathf.Clamp01(b.age/1.35f);
                    b.rect.anchoredPosition=Vector2.Lerp(b.from,hover,Mathf.SmoothStep(0,1,t))
                        +Vector2.right*Mathf.Sin(t*Mathf.PI)*(i%2==0?-35:35);
                    if(t>=1){b.state=0;b.age=0;b.wait=IsBoss ? 1.5f : 2.3f+(i%3)*.45f;}
                }
                float tilt=b.state==1?Mathf.Clamp(Mathf.Atan2(b.strike.y-b.from.y,b.strike.x-b.from.x)*Mathf.Rad2Deg+135,-35,35):Mathf.Sin(elapsed*2+i)*7;
                b.rect.localRotation=Quaternion.Euler(0,0,tilt);
                if (b.animation) b.animation.SetAttacking(b.state == 1);
                b.rect.localScale=Vector3.one;
                PositionBar(b);
                b.burst.gameObject.SetActive(b.state==2 && b.age<.25f && b.flash>0);
                b.burst.color=new Color(1,.76f,.28f,Mathf.Clamp01(1-b.age/.25f));
                b.burst.rectTransform.localScale=Vector3.one*(.6f+b.age*3);
            }
        }
    }
}
