using UnityEngine;
using UnityEngine.UI;

namespace SkyTree
{
    // Seven persistent birds alternate between circling, diving and pulling away.
    public sealed class SkybirdSwarm : MonoBehaviour
    {
        private sealed class Bird
        {
            public RectTransform rect;
            public RawImage image;
            public BattleEffectGraphic burst;
            public Vector2 home, from, strike;
            public int state;
            public float age, wait, flash;
        }
        private readonly Bird[] birds = new Bird[7];
        private BattleShipView player;
        private RectTransform space;
        private float elapsed;
        public int CollisionCount { get; private set; }
        public int RepelledCount { get; private set; }
        public int Count => birds.Length;

        public void Build(BattleShipView ship, RawImage prefab)
        {
            player = ship; space = (RectTransform)transform;
            Vector2[] homes = {new Vector2(-135,205),new Vector2(-65,240),new Vector2(5,200),
                new Vector2(80,235),new Vector2(140,175),new Vector2(-100,140),new Vector2(60,140)};
            for(int i=0;i<birds.Length;i++)
            {
                var image = Instantiate(prefab, space, false);
                image.name = "Skybird " + (i+1);
                image.rectTransform.sizeDelta *= 1f + (i%3) * (3f/46f);
                image.rectTransform.anchoredPosition=homes[i];
                var burst = new GameObject("Collision flash "+(i+1),typeof(RectTransform)).AddComponent<BattleEffectGraphic>();
                burst.transform.SetParent(space,false); burst.rectTransform.sizeDelta=Vector2.one*32;
                burst.star=true; burst.raycastTarget=false; burst.gameObject.SetActive(false);
                birds[i]=new Bird {rect=image.rectTransform,image=image,home=homes[i],wait=1.4f+i*.62f,burst=burst};
            }
        }

        public RectTransform GetTarget(int index) => birds[index%birds.Length].rect;

        public void Hit(RectTransform target)
        {
            foreach(var bird in birds)
                if(bird.rect==target)
                {
                    bird.from=bird.rect.anchoredPosition; bird.state=2; bird.age=0; bird.flash=.3f;
                    bird.burst.rectTransform.anchoredPosition=bird.from;
                    RepelledCount++; return;
                }
        }

        private void Update()
        {
            if(!player) return;
            float dt=Time.deltaTime; elapsed+=dt;
            for(int i=0;i<birds.Length;i++)
            {
                var b=birds[i]; b.age+=dt; b.flash=Mathf.Max(0,b.flash-dt);
                b.image.color=Color.Lerp(Color.white,new Color(1,.45f,.3f,1),b.flash/.3f);
                Vector2 hover=b.home+new Vector2(Mathf.Sin(elapsed*1.3f+i)*9,Mathf.Sin(elapsed*2+i*1.7f)*5);
                if(b.state==0)
                {
                    b.rect.anchoredPosition=hover;
                    if(b.age>=b.wait)
                    {
                        b.state=1;b.age=0;b.from=b.rect.anchoredPosition;
                        // Aim at the visible bow, side rail and aft deck, not the tree canopy.
                        Vector2[] deck={new Vector2(-135,70),new Vector2(130,-55),new Vector2(-75,-65)};
                        b.strike=space.InverseTransformPoint(player.transform.TransformPoint(deck[i%3]));
                    }
                }
                else if(b.state==1)
                {
                    float t=Mathf.Clamp01(b.age/.9f);
                    b.rect.anchoredPosition=Vector2.Lerp(b.from,b.strike,t*t);
                    if(t>=1)
                    {
                        CollisionCount++;player.ReactToImpact();
                        b.burst.rectTransform.anchoredPosition=b.strike;
                        b.flash=.25f; b.state=2;b.age=0;b.from=b.strike;
                    }
                }
                else
                {
                    float t=Mathf.Clamp01(b.age/1.35f);
                    b.rect.anchoredPosition=Vector2.Lerp(b.from,hover,Mathf.SmoothStep(0,1,t))
                        +Vector2.right*Mathf.Sin(t*Mathf.PI)*(i%2==0?-35:35);
                    if(t>=1){b.state=0;b.age=0;b.wait=2.3f+(i%3)*.45f;}
                }
                float tilt=b.state==1?Mathf.Clamp(Mathf.Atan2(b.strike.y-b.from.y,b.strike.x-b.from.x)*Mathf.Rad2Deg+135,-35,35):Mathf.Sin(elapsed*2+i)*7;
                b.rect.localRotation=Quaternion.Euler(0,0,tilt);
                float flap=Mathf.Sin(elapsed*12+i*1.4f);
                b.rect.localScale=new Vector3(1+flap*.025f,1+flap*.08f,1);
                b.burst.gameObject.SetActive(b.state==2 && b.age<.25f && b.flash>0);
                b.burst.color=new Color(1,.76f,.28f,Mathf.Clamp01(1-b.age/.25f));
                b.burst.rectTransform.localScale=Vector3.one*(.6f+b.age*3);
            }
        }
    }
}
