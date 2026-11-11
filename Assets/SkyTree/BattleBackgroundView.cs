using UnityEngine;
using UnityEngine.UI;

namespace SkyTree
{
    // Screen-space velocity: positive X moves right; negative Y moves down.
    public sealed class BattleBackgroundView : MonoBehaviour
    {
        [SerializeField] private RawImage ocean;
        [SerializeField] private Vector2 scrollVelocity = new Vector2(12f, -12f);
        [SerializeField, Min(100f)] private float tileSize = 720f;
        [SerializeField] private RectTransform[] clouds;
        [SerializeField] private float cloudSpeedMultiplier = 1.45f;
        private Vector2 uvOffset;

        private void OnEnable() => UpdateOcean();

        private void Update() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0) return;
            // Moving UVs opposite the requested velocity moves the painting with it.
            uvOffset -= scrollVelocity * (deltaTime / Mathf.Max(100, tileSize));
            uvOffset.x = Mathf.Repeat(uvOffset.x, 1f);
            uvOffset.y = Mathf.Repeat(uvOffset.y, 1f);
            UpdateOcean();
            if (clouds == null) return;
            var bounds = ((RectTransform)transform).rect;
            for (int i = 0; i < clouds.Length; i++)
            {
                var cloud = clouds[i];
                if (!cloud) continue;
                Vector2 p = cloud.anchoredPosition + scrollVelocity * (cloudSpeedMultiplier + i * .12f) * deltaTime;
                float width = bounds.width + cloud.sizeDelta.x;
                float height = bounds.height + cloud.sizeDelta.y;
                p.x = Mathf.Repeat(p.x + width * .5f, width) - width * .5f;
                p.y = Mathf.Repeat(p.y + height * .5f, height) - height * .5f;
                cloud.anchoredPosition = p;
            }
        }

        private void UpdateOcean()
        {
            if (!ocean) return;
            var size = ((RectTransform)transform).rect.size;
            ocean.uvRect = new Rect(uvOffset.x, uvOffset.y,
                size.x / Mathf.Max(100, tileSize), size.y / Mathf.Max(100, tileSize));
        }

        private void OnRectTransformDimensionsChange() => UpdateOcean();
    }
}
