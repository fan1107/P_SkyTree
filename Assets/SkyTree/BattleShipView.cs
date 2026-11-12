using System;
using UnityEngine;

namespace SkyTree
{
    public sealed class BattleShipView : MonoBehaviour
    {
        [SerializeField] private GameObject captainRig;
        [SerializeField] private RectTransform captainAnchor;
        [SerializeField] private float captainPixelsPerUnit = 20f;
        [SerializeField] private GameObject navigatorRig;
        [SerializeField] private RectTransform navigatorAnchor;
        [SerializeField] private float navigatorPixelsPerUnit = 17f;
        [SerializeField, Min(1)] private int maximumHealth = 1000;
        // Normalized points along the upper silhouette of HullReverseMatched.
        [SerializeField] private Vector2[] upperHullEdge = {
            new Vector2(.12f, .895f), new Vector2(.25f, .88f), new Vector2(.36f, .825f),
            new Vector2(.50f, .73f), new Vector2(.65f, .62f), new Vector2(.77f, .65f), new Vector2(.90f, .55f)
        };
        private RectTransform hull;
        private CanvasCrewRig[] crew;
        private Vector2 origin;
        private Quaternion rotation;
        private float entered, hitTime = -100;
        private bool initialized;
        public event Action<int> CannonFire;
        public BattleHealth Health { get; private set; }
        public CanvasCrewRig[] Crew => crew;
        public void ReactToImpact() => hitTime = Time.time;
        public int TakeDamage(int damage)
        {
            int applied = Health == null ? 0 : Health.Damage(damage);
            if (applied > 0) ReactToImpact();
            return applied;
        }
        public Vector3 GetUpperHullContact(float across)
        {
            float point = Mathf.Clamp01(across) * (upperHullEdge.Length - 1);
            int index = Mathf.Min(Mathf.FloorToInt(point), upperHullEdge.Length - 2);
            Vector2 uv = Vector2.Lerp(upperHullEdge[index], upperHullEdge[index + 1], point - index);
            Rect r = hull.rect;
            return hull.TransformPoint(new Vector3(Mathf.Lerp(r.xMin, r.xMax, uv.x), Mathf.Lerp(r.yMin, r.yMax, uv.y), 0));
        }
        private void Start() => InitializeCrew();
        public void InitializeCrew()
        {
            if (initialized) return;
            if (!captainRig || !navigatorRig || !captainAnchor || !navigatorAnchor)
                throw new InvalidOperationException("BattleShip requires both crew prefabs and stations.");
            initialized = true;
            Health = new BattleHealth(Mathf.Max(1, maximumHealth));
            hull = (RectTransform)transform.Find("Hull");
            origin = ((RectTransform)transform).anchoredPosition;
            rotation = transform.localRotation; entered = Time.time;
            crew = new CanvasCrewRig[2];
            crew[0] = captainAnchor.gameObject.AddComponent<CanvasCrewRig>();
            crew[1] = navigatorAnchor.gameObject.AddComponent<CanvasCrewRig>();
            crew[0].Build(captainRig, captainPixelsPerUnit);
            crew[1].Build(navigatorRig, navigatorPixelsPerUnit);
            crew[0].Fired += CaptainFired; crew[1].Fired += NavigatorFired;
        }
        private void CaptainFired() => CannonFire?.Invoke(0);
        private void NavigatorFired() => CannonFire?.Invoke(1);
        public bool BeginCannonAttack(int index) => Health != null && Health.IsAlive && crew != null && index >= 0 && index < crew.Length && crew[index].BeginAttack();
        private void LateUpdate()
        {
            float t = Time.time - entered;
            var root = (RectTransform)transform;
            root.anchoredPosition = origin + Vector2.up * (3.5f * Mathf.Sin(t * .75f));
            root.localRotation = rotation * Quaternion.Euler(0, 0, .5f * Mathf.Sin(t * .6f));
            float age = Time.time - hitTime, shake = Mathf.Exp(-age * 10f);
            root.anchoredPosition += new Vector2(Mathf.Sin(age * 65) * 2.5f, Mathf.Sin(age * 49) * 1.5f) * shake;
            root.localRotation *= Quaternion.Euler(0, 0, Mathf.Sin(age * 60) * .8f * shake);
        }
        private void OnDestroy()
        {
            if (crew != null)
            {
                if (crew[0]) crew[0].Fired -= CaptainFired;
                if (crew[1]) crew[1].Fired -= NavigatorFired;
            }
            CannonFire = null;
        }
    }
}
