using UnityEngine;
using UnityEngine.UI;

namespace SkyTree
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BattleHealthBar : MaskableGraphic
    {
        private BattleHealth health;
        private bool friendly;
        private float fraction = 1, delayed = 1, delay;
        public float Fraction => fraction;

        public void Bind(BattleHealth value, bool isFriendly)
        {
            if (health != null) health.Changed -= Refresh;
            health = value;
            friendly = isFriendly;
            fraction = delayed = health.Fraction;
            raycastTarget = false;
            health.Changed += Refresh;
            SetVerticesDirty();
        }

        private void Refresh()
        {
            fraction = health.Fraction;
            delay = .25f;
            SetVerticesDirty();
        }

        private void Update()
        {
            if (delay > 0) { delay -= Time.deltaTime; return; }
            if (Mathf.Approximately(delayed, fraction)) return;
            delayed = Mathf.MoveTowards(delayed, fraction, Time.deltaTime * .55f);
            SetVerticesDirty();
        }

        protected override void OnDestroy()
        {
            if (health != null) health.Changed -= Refresh;
            base.OnDestroy();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            float rim = friendly ? 1.4f : .65f;
            BattleHudGraphic.Rounded(vh, r, r.height * .5f, new Color32(255, 219, 125, 255), new Color32(122, 68, 28, 255));
            var track = BattleHudGraphic.Inset(r, rim);
            BattleHudGraphic.Rounded(vh, track, track.height * .5f, new Color32(28, 29, 23, 255), new Color32(7, 39, 39, 255));
            var fill = BattleHudGraphic.Inset(track, friendly ? 1.3f : .65f);
            float width = fill.width;
            fill.width = width * delayed;
            BattleHudGraphic.Rounded(vh, fill, fill.height * .5f, new Color32(255, 224, 139, 255), new Color32(204, 154, 66, 255));
            fill.width = width * fraction;
            Color top = friendly ? new Color32(114, 247, 102, 255) : new Color32(255, 154, 115, 255);
            Color bottom = friendly ? new Color32(12, 163, 61, 255) : new Color32(183, 36, 32, 255);
            if (friendly && fraction <= .25f) { top = new Color32(255, 138, 104, 255); bottom = new Color32(184, 37, 29, 255); }
            BattleHudGraphic.Rounded(vh, fill, fill.height * .5f, top, bottom);
        }
    }
}
