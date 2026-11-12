using UnityEngine;
using UnityEngine.UI;

namespace SkyTree
{
    // Resolution-independent brass/teal frames matching the approved combat HUD.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BattleHudGraphic : MaskableGraphic
    {
        public bool emblem;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            if (emblem)
            {
                Shield(vh, r, new Color32(255, 208, 102, 255));
                Shield(vh, Inset(r, 1.5f), new Color32(12, 79, 77, 255));
                var c = r.center;
                Polygon(vh, new[] { c + new Vector2(-7, -3), c + new Vector2(7, -3), c + new Vector2(4, -7), c + new Vector2(-4, -7) }, new Color32(255, 222, 144, 255));
                Rounded(vh, new Rect(c.x - .7f, c.y - 3, 1.4f, 13), 0, Color.white, new Color32(255, 222, 144, 255));
                Polygon(vh, new[] { c + new Vector2(1, 9), c + new Vector2(1, -1), c + new Vector2(7, -1) }, new Color32(255, 222, 144, 255));
                return;
            }
            Rounded(vh, r, 9, new Color32(93, 55, 22, 255), new Color32(54, 32, 17, 255));
            Rounded(vh, Inset(r, 1), 8, new Color32(255, 226, 137, 255), new Color32(163, 101, 34, 255));
            Rounded(vh, Inset(r, 2.5f), 6.5f, new Color32(169, 109, 36, 255), new Color32(255, 207, 91, 255));
            Rounded(vh, Inset(r, 3.5f), 5.5f, new Color32(255, 224, 140, 255), new Color32(107, 68, 25, 255));
            Rounded(vh, Inset(r, 5), 4, new Color32(8, 99, 99, 255), new Color32(5, 48, 50, 255));
        }

        internal static Rect Inset(Rect r, float amount) => new Rect(r.x + amount, r.y + amount, r.width - amount * 2, r.height - amount * 2);

        internal static void Rounded(VertexHelper vh, Rect r, float radius, Color top, Color bottom)
        {
            if (r.width <= 0 || r.height <= 0) return;
            radius = Mathf.Clamp(radius, 0, Mathf.Min(r.width, r.height) * .5f);
            int start = vh.currentVertCount;
            vh.AddVert(r.center, Color.Lerp(bottom, top, .5f), Vector2.zero);
            const int steps = 6;
            for (int corner = 0; corner < 4; corner++)
            {
                var center = new Vector2(corner == 0 || corner == 3 ? r.xMax - radius : r.xMin + radius,
                    corner < 2 ? r.yMax - radius : r.yMin + radius);
                for (int j = 0; j <= steps; j++)
                {
                    float angle = (corner * 90 + j * 90f / steps) * Mathf.Deg2Rad;
                    var p = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    vh.AddVert(p, Color.Lerp(bottom, top, Mathf.InverseLerp(r.yMin, r.yMax, p.y)), Vector2.zero);
                }
            }
            int count = 4 * (steps + 1);
            for (int i = 0; i < count; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % count);
        }

        static void Shield(VertexHelper vh, Rect r, Color tint)
        {
            Polygon(vh, new[] { new Vector2(r.center.x, r.yMax), new Vector2(r.xMax, r.yMax - r.height * .15f),
                new Vector2(r.xMax - r.width * .08f, r.yMin + r.height * .28f), new Vector2(r.center.x, r.yMin),
                new Vector2(r.xMin + r.width * .08f, r.yMin + r.height * .28f), new Vector2(r.xMin, r.yMax - r.height * .15f) }, tint);
        }

        static void Polygon(VertexHelper vh, Vector2[] points, Color tint)
        {
            int start = vh.currentVertCount;
            foreach (var p in points) vh.AddVert(p, tint, Vector2.zero);
            for (int i = 1; i < points.Length - 1; i++) vh.AddTriangle(start, start + i, start + i + 1);
        }
    }
}
