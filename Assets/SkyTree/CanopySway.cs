using UnityEngine;

namespace SkyTree
{
    // All clusters share a 14-second calm / gust / settling cycle.
    internal static class FoliageWind
    {
        public static float Gust(float time)
        {
            float cycle = Mathf.Repeat(time, 14f);
            return Mathf.SmoothStep(0, 1, (cycle - 5f) / 3f)
                * (1f - Mathf.SmoothStep(0, 1, (cycle - 10f) / 4f));
        }
    }

    public sealed class CanopySway : MonoBehaviour
    {
        [SerializeField, Range(0, 3)] private float amplitude = 1.1f;
        [SerializeField] private float speed = .8f;
        [SerializeField] private float phase;
        private Quaternion rest;
        private float started;

        private void OnEnable() { rest = transform.localRotation; started = Time.time; }
        private void OnDisable() { transform.localRotation = rest; }

        private void Update()
        {
            float t = Time.time;
            float gust = FoliageWind.Gust(t);
            float offset = phase + transform.GetSiblingIndex() * .43f;
            float wind = Mathf.Sin(t * speed + offset) * .8f
                + Mathf.Sin(t * speed * .53f + offset * 1.7f) * .2f;
            float rustle = (Mathf.Sin(t * 4.8f + offset) + .3f * Mathf.Sin(t * 8.3f + offset))
                * Mathf.Lerp(.06f, .4f, gust);
            float angle = amplitude * (wind * Mathf.Lerp(.55f, 2.5f, gust) + rustle);
            transform.localRotation = rest * Quaternion.Euler(0, 0,
                angle * Mathf.SmoothStep(0, 1, (t - started) / 2f));
        }
    }
}
