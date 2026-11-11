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
        [SerializeField] private Vector2[] deckBoundary = {
            new Vector2(-43,-49), new Vector2(-13,-28), new Vector2(68,-63),
            new Vector2(60,-76), new Vector2(48,-95), new Vector2(-28,-67) };
        [SerializeField] private Vector2 treeBase = new Vector2(-10,-51);
        [SerializeField] private float treeClearance = 20;
        [SerializeField] private Vector2 cannonBase = new Vector2(93,-99);
        [SerializeField] private float cannonClearance = 32;
        [SerializeField] private Vector2[] stops = {
            new Vector2(20,-76), new Vector2(46,-82), new Vector2(50,-62),
            new Vector2(27,-48), new Vector2(-23,-68), new Vector2(-32,-48),
            new Vector2(-31,-61), new Vector2(-8,-74) };
        private RectTransform actor, drawing;
        private Transform skeleton;
        private Animator animator;
        private readonly List<Transform> sources = new List<Transform>();
        private readonly List<RectTransform> images = new List<RectTransform>();
        private readonly Queue<Vector2> route = new Queue<Vector2>();
        private float wait = 1.3f, speed;
        private int currentStop, visits;
        public Vector2 FootPosition => actor ? actor.anchoredPosition : stops[0];
        public bool IsMoving => route.Count > 0;

        private void Start() => Initialize();

        public void Initialize()
        {
            if (actor || !rigPrefab || !ship) return;
            if (staticCaptain) staticCaptain.SetActive(false);
            actor = NewRect("Wandering Captain", ship);
            actor.anchoredPosition = stops[0];
            drawing = NewRect("Animated Parts", actor);
            skeleton = Instantiate(rigPrefab, actor, false).transform;
            skeleton.name = "Skeleton";
            skeleton.localPosition = Vector3.zero;
            skeleton.localScale = Vector3.one;
            animator = skeleton.GetComponent<Animator>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var renderers = skeleton.GetComponentsInChildren<SpriteRenderer>();
            System.Array.Sort(renderers, (a,b) => a.sortingOrder.CompareTo(b.sortingOrder));
            foreach (var renderer in renderers)
            {
                renderer.enabled = false;
                var rect = NewRect(renderer.transform.parent.name, drawing);
                rect.sizeDelta = renderer.sprite.bounds.size;
                rect.pivot = renderer.sprite.pivot / renderer.sprite.rect.size;
                var image = rect.gameObject.AddComponent<Image>();
                image.sprite = renderer.sprite;
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
            if (!actor) return;
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
            int destination = order[visits++ % order.Length];
            var path = FindRoute(currentStop, destination);
            if (path.Count == 0) { wait = .5f; return; }
            foreach (int index in path) route.Enqueue(stops[index]);
            currentStop = destination;
            bool run = visits % 4 == 0;
            speed = run ? 23 : 13;
            animator.CrossFadeInFixedTime(run ? "Run" : "Walk", .18f);
        }

        private void Rest()
        {
            wait = 1.6f + (visits % 3) * .65f;
            animator.CrossFadeInFixedTime("Breathe", .18f);
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
            return inside && Vector2.Distance(point,treeBase)>=treeClearance &&
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
                images[i].anchoredPosition = (Vector2)skeleton.InverseTransformPoint(sources[i].position)*pixelsPerUnit;
                images[i].localRotation = Quaternion.Inverse(skeleton.rotation)*sources[i].rotation;
                Vector3 s=sources[i].lossyScale, root=skeleton.lossyScale;
                images[i].localScale=new Vector3(s.x/root.x,s.y/root.y,1)*pixelsPerUnit;
            }
            if(tree)
            {
                // Behind the trunk when farther up the deck; in front when nearer the camera.
                bool behind=actor.anchoredPosition.y>treeBase.y;
                int index=tree.GetSiblingIndex();
                if(actor.GetSiblingIndex()<index) index--;
                actor.SetSiblingIndex(behind ? index : index+1);
            }
        }

        private void OnDestroy()
        {
            if(actor) Destroy(actor.gameObject);
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
