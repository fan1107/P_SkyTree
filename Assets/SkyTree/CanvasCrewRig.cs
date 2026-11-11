using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkyTree
{
    // Projects a cutout prefab into an overlay Canvas without world sprites.
    [DefaultExecutionOrder(200)]
    public sealed class CanvasCrewRig : MonoBehaviour
    {
        private readonly List<Transform> sources = new List<Transform>();
        private readonly List<RectTransform> images = new List<RectTransform>();
        private Transform skeleton;
        private float pixelsPerUnit;
        private CaptainAttackAnimation captain;
        private FemaleNavigatorAnimation navigator;
        public Animator Animator { get; private set; }
        public event Action Fired;
        public int FireEvents { get; private set; }
        public int LastFireFrame { get; private set; } = -1;
        public bool Ready => Animator && Animator.isActiveAndEnabled;
        public void Build(GameObject prefab, float scale)
        {
            if (skeleton) return;
            pixelsPerUnit = scale;
            skeleton = Instantiate(prefab, transform, false).transform;
            skeleton.name = "Skeleton";
            Animator = skeleton.GetComponent<Animator>();
            Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Animator.Play("Breathe", 0, 0);
            captain = skeleton.GetComponent<CaptainAttackAnimation>();
            navigator = skeleton.GetComponent<FemaleNavigatorAnimation>();
            if (captain) captain.onCannonFire.AddListener(OnFire);
            if (navigator) navigator.onCannonFire.AddListener(OnFire);
            var renderers = skeleton.GetComponentsInChildren<SpriteRenderer>(true);
            Array.Sort(renderers, (a,b) => a.sortingOrder.CompareTo(b.sortingOrder));
            foreach (var renderer in renderers)
            {
                renderer.enabled = false;
                if (!renderer.sprite) continue;
                var rect = new GameObject(renderer.transform.parent.name, typeof(RectTransform)).GetComponent<RectTransform>();
                rect.SetParent(transform, false);
                rect.sizeDelta = renderer.sprite.bounds.size;
                rect.pivot = renderer.sprite.pivot / renderer.sprite.rect.size;
                var image = rect.gameObject.AddComponent<Image>();
                image.sprite = renderer.sprite; image.raycastTarget = false;
                image.material = renderer.sharedMaterial;
                sources.Add(renderer.transform); images.Add(rect);
            }
            Sync();
        }
        public bool BeginAttack()
        {
            if (!Ready || Animator.IsInTransition(0) || Animator.GetCurrentAnimatorStateInfo(0).IsName("Attack")) return false;
            // The clip event is the shot's source of truth, rather than an independent timer.
            Animator.Play("Attack", 0, 0);
            return true;
        }
        private void OnFire() { FireEvents++; LastFireFrame = Time.frameCount; Fired?.Invoke(); }
        private void LateUpdate() => Sync();
        public void Sync()
        {
            if (!skeleton) return;
            for (int i=0;i<sources.Count;i++)
            {
                var source = sources[i]; var image = images[i];
                image.gameObject.SetActive(source.gameObject.activeInHierarchy);
                image.anchoredPosition = (Vector2)skeleton.InverseTransformPoint(source.position) * pixelsPerUnit;
                image.localRotation = Quaternion.Inverse(skeleton.rotation) * source.rotation;
                var s = source.lossyScale; var root = skeleton.lossyScale;
                image.localScale = new Vector3(s.x/root.x, s.y/root.y, 1) * pixelsPerUnit;
            }
        }
        private void OnDestroy()
        {
            if (captain) captain.onCannonFire.RemoveListener(OnFire);
            if (navigator) navigator.onCannonFire.RemoveListener(OnFire);
            Fired = null;
        }
    }
}
