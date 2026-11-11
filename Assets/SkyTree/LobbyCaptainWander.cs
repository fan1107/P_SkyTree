using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkyTree
{
    // Coordinates are foot positions in MainShip_Step01 local UI units.
    // The boundary is inset from the painted rail by the boot/stride clearance.
    public sealed class LobbyCaptainWander : MonoBehaviour
    {
        [SerializeField] private GameObject rigPrefab;
        [SerializeField] private RectTransform ship;
        [SerializeField] private Transform tree;
        [SerializeField] private GameObject staticCaptain;
        [SerializeField] private float pixelsPerUnit = 15;
        [SerializeField] private string actorName = "Wandering Captain";
        [SerializeField] private int routeOffset;
        [SerializeField] private float initialWait = 1.3f;
        [SerializeField] private float pace = 1f;
        [SerializeField] private Vector2[] deckBoundary = {
            new Vector2(-43,-49), new Vector2(-13,-28), new Vector2(68,-63),
            new Vector2(60,-76), new Vector2(48,-95), new Vector2(-28,-67) };
        [SerializeField] private Vector2 treeBase = new Vector2(-10,-51);
        [SerializeField] private float treeClearance = 36;
        [SerializeField] private float canopyRightEdge = 42;
        [SerializeField] private float canopyFrontEdge = -85;
        [SerializeField] private Vector2 cannonBase = new Vector2(93,-99);
        [SerializeField] private float cannonClearance = 32;
        [SerializeField] private Vector2[] stops = {
            new Vector2(42,-84), new Vector2(50,-82), new Vector2(55,-69),
            new Vector2(46,-76), new Vector2(36,-87), new Vector2(42,-90),
            new Vector2(50,-88), new Vector2(55,-78) };
        private RectTransform actor, drawing;
        private Transform skeleton;
        private Animator animator;
        private FemaleNavigatorAnimation navigator;
        private readonly List<Transform> sources = new List<Transform>();
        private readonly List<RectTransform> images = new List<RectTransform>();
        private readonly Queue<Vector2> route = new Queue<Vector2>();
        private float wait = 1.3f, speed;
        private int currentStop, visits;
        public Vector2 FootPosition => actor ? actor.anchoredPosition : stops[0];
        public bool IsMoving => route.Count > 0;
        public string CrewName => actorName;
        public int Visits => visits;

        private void OnEnable()
        {
            // Unity restores scene objects after a script reload, but not these runtime lists.
            if (Application.isPlaying) Initialize();
        }

        private void Start() => Initialize();

        public void Initialize()
        {
            if (!rigPrefab || !ship) return;
            if (actor && animator && sources.Count > 0 && sources.Count == images.Count) return;
            // Rebuild a stale runtime character instead of keeping frozen UI body parts.
            var previous = actor ? actor : ship.Find(actorName) as RectTransform;
            if (previous)
            {
                previous.gameObject.SetActive(false);
                previous.name = "Retired " + actorName;
                if (Application.isPlaying) Destroy(previous.gameObject);
                else DestroyImmediate(previous.gameObject);
            }
            sources.Clear();
            images.Clear();
            route.Clear();
            visits = 0;
            currentStop = Mathf.Clamp(routeOffset, 0, stops.Length - 1);
            wait = initialWait;
            speed = 0;
            if (staticCaptain) staticCaptain.SetActive(false);
            var cannon = ship.Find("Cannon") as RectTransform;
            if (cannon)
            {
                Vector3 bottom = cannon.TransformPoint(new Vector3(0, cannon.rect.yMin, 0));
                cannonBase = ship.InverseTransformPoint(bottom);
            }
            actor = NewRect(actorName, ship);
            actor.anchoredPosition = stops[currentStop];
            drawing = NewRect("Animated Parts", actor);
            skeleton = Instantiate(rigPrefab, actor, false).transform;
            skeleton.name = "Skeleton";
            skeleton.localPosition = Vector3.zero;
            skeleton.localScale = Vector3.one;
            animator = skeleton.GetComponent<Animator>();
            navigator = skeleton.GetComponent<FemaleNavigatorAnimation>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            animator.speed = 1f;
            var renderers = skeleton.GetComponentsInChildren<SpriteRenderer>(true);
            System.Array.Sort(renderers, (a,b) => a.sortingOrder.CompareTo(b.sortingOrder));
            foreach (var renderer in renderers)
            {
                renderer.enabled = false;
                var rect = NewRect(renderer.transform.parent.name, drawing);
                rect.sizeDelta = renderer.sprite.bounds.size;
                rect.pivot = renderer.sprite.pivot / renderer.sprite.rect.size;
                var image = rect.gameObject.AddComponent<Image>();
                image.sprite = renderer.sprite;
                image.material = renderer.sharedMaterial;
                image.raycastTarget = false;
                sources.Add(renderer.transform);
                images.Add(rect);
            }
            UpdateDrawing();
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        private void Update() => Advance(Time.deltaTime);

        public void Advance(float dt)
        {
            if (!actor || !animator || sources.Count == 0) Initialize();
            if (!actor || !animator) return;
            if (route.Count == 0)
            {
                wait -= dt;
                if (wait <= 0) BeginRoute();
                return;
            }
            Vector2 target = route.Peek();
            Vector2 direction = target - actor.anchoredPosition;
            if (Mathf.Abs(direction.x) > .2f)
                drawing.localScale = new Vector3(direction.x < 0 ? -1 : 1, 1, 1);
            Vector2 next = Vector2.MoveTowards(actor.anchoredPosition, target, speed * Mathf.Min(dt,.1f));
            // This guard also blocks an invalid route after inspector edits.
            if (!IsSegmentSafe(actor.anchoredPosition, next))
            {
                route.Clear(); Rest(); return;
            }
            actor.anchoredPosition = next;
            if ((next-target).sqrMagnitude < .01f)
            {
                route.Dequeue();
                if (route.Count == 0) Rest();
            }
        }

        private void BeginRoute()
        {
            // Cycle through varied destinations; use visibility-graph routing around the tree.
            int[] order = {1,3,5,7,2,4,6,0};
            int destination = order[(visits++ + routeOffset) % order.Length] % stops.Length;
            var path = FindRoute(currentStop, destination);
            if (path.Count == 0) { wait = .5f; return; }
            foreach (int index in path) route.Enqueue(stops[index]);
            currentStop = destination;
            bool run = visits % 4 == 0;
            speed = (run ? 23 : 13) * pace;
            SetMotion(run ? 2 : 1);
        }

        private void Rest()
        {
            wait = 1.6f + (visits % 3) * .65f;
            SetMotion(0);
        }

        private void SetMotion(int mode)
        {
            if (navigator) navigator.SetMovement(mode);
            else animator.CrossFadeInFixedTime(mode == 0 ? "Breathe" : mode == 1 ? "Walk" : "Run", .18f);
        }

        public List<int> FindRoute(int from, int to)
        {
            var previous = new int[stops.Length];
            for (int i=0;i<previous.Length;i++) previous[i]=-1;
            var queue = new Queue<int>(); queue.Enqueue(from); previous[from]=from;
            while(queue.Count>0)
            {
                int node=queue.Dequeue();
                for(int i=0;i<stops.Length;i++)
                    if(previous[i]<0 && IsSegmentSafe(stops[node],stops[i]))
                    { previous[i]=node; queue.Enqueue(i); }
            }
            var result=new List<int>();
            if(previous[to]<0) return result;
            for(int node=to;node!=from;node=previous[node]) result.Add(node);
            result.Reverse(); return result;
        }

        public bool IsWalkable(Vector2 point)
        {
            bool inside=false;
            for(int i=0,j=deckBoundary.Length-1;i<deckBoundary.Length;j=i++)
            {
                Vector2 a=deckBoundary[i],b=deckBoundary[j];
                if((a.y>point.y)!=(b.y>point.y) && point.x<(b.x-a.x)*(point.y-a.y)/(b.y-a.y)+a.x)
                    inside=!inside;
            }
            // The upper-left canopy hides the face before the feet reach the trunk.
            bool faceHidden = point.x < canopyRightEdge && point.y > canopyFrontEdge;
            return inside && !faceHidden && Vector2.Distance(point,treeBase)>=treeClearance &&
                Vector2.Distance(point,cannonBase)>=cannonClearance;
        }

        public bool IsSegmentSafe(Vector2 a, Vector2 b)
        {
            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/.5f));
            for(int i=0;i<=steps;i++) if(!IsWalkable(Vector2.Lerp(a,b,(float)i/steps))) return false;
            return true;
        }

        private void LateUpdate() => UpdateDrawing();

        public void UpdateDrawing()
        {
            if (!actor) return;
            for(int i=0;i<sources.Count;i++)
            {
                images[i].gameObject.SetActive(sources[i].gameObject.activeInHierarchy);
                images[i].anchoredPosition = (Vector2)skeleton.InverseTransformPoint(sources[i].position)*pixelsPerUnit;
                images[i].localRotation = Quaternion.Inverse(skeleton.rotation)*sources[i].rotation;
                Vector3 s=sources[i].lossyScale, root=skeleton.lossyScale;
                images[i].localScale=new Vector3(s.x/root.x,s.y/root.y,1)*pixelsPerUnit;
            }
            // Keep the separate leaf prefab above every ship occupant.
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
            if (actor.GetSiblingIndex() != targetIndex) actor.SetSiblingIndex(targetIndex);
            // The lower crew member draws in front when the two paths cross in perspective.
            foreach (var other in GetComponents<LobbyCaptainWander>())
                if (other != this && other.actor && other.ship == ship &&
                    actor.anchoredPosition.y > other.actor.anchoredPosition.y && actor.GetSiblingIndex() > other.actor.GetSiblingIndex())
                    actor.SetSiblingIndex(other.actor.GetSiblingIndex());
        }

        private void OnDestroy()
        {
            if (actor)
            {
                if (Application.isPlaying) Destroy(actor.gameObject);
                else DestroyImmediate(actor.gameObject);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if(!ship || deckBoundary.Length<3)return;
            Gizmos.color=Color.green;
            for(int i=0;i<deckBoundary.Length;i++)
                Gizmos.DrawLine(ship.TransformPoint(deckBoundary[i]),ship.TransformPoint(deckBoundary[(i+1)%deckBoundary.Length]));
            Gizmos.color=Color.red;
            foreach(var point in stops) Gizmos.DrawWireSphere(ship.TransformPoint(point),2);
        }
    }
}
