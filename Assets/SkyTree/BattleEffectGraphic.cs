using UnityEngine;
using UnityEngine.UI;

namespace SkyTree
{
    // Small code-drawn light discs, sparks and smoke; no per-frame textures.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BattleEffectGraphic : MaskableGraphic
    {
        public bool star;
        public bool solid;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            const int count = 32;
            var r = rectTransform.rect;
            var v = UIVertex.simpleVert;
            v.position = r.center; v.color = color; vh.AddVert(v);
            for (int i = 0; i <= count; i++)
            {
                float angle = i * Mathf.PI * 2 / count;
                float radius = star && i % 4 != 0 ? .4f : 1f;
                v.position = r.center + new Vector2(Mathf.Cos(angle) * r.width, Mathf.Sin(angle) * r.height) * (.5f * radius);
                v.color = solid ? color : new Color(color.r, color.g, color.b, 0);
                vh.AddVert(v);
                if (i > 0) vh.AddTriangle(0, i, i + 1);
            }
        }
    }
}
