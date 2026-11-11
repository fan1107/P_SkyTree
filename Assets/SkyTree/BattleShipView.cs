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
        private CanvasCrewRig[] crew;
        private Vector2 origin;
        private Quaternion rotation;
        private float entered, hitTime = -100;
        private bool initialized;
        public event Action<int> CannonFire;
        public CanvasCrewRig[] Crew => crew;
        public void ReactToImpact() => hitTime = Time.time;
        private void Start() => InitializeCrew();
        public void InitializeCrew()
        {
            if (initialized) return;
            if (!captainRig || !navigatorRig || !captainAnchor || !navigatorAnchor)
                throw new InvalidOperationException("BattleShip requires both crew prefabs and stations.");
            initialized = true;
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
        public bool BeginCannonAttack(int index) => crew != null && index >= 0 && index < crew.Length && crew[index].BeginAttack();
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
