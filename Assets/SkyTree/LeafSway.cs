using UnityEngine;

namespace SkyTree
{
    public sealed class LeafSway : MonoBehaviour
    {
        [SerializeField] private float phase;
        private Quaternion rest;
        private float started;

        private void OnEnable() { rest = transform.localRotation; started = Time.time; }
        private void OnDisable() { transform.localRotation = rest; }

        private void Update()
        {
            float t = Time.time;
            float gust = FoliageWind.Gust(t);
            float variation = Mathf.Lerp(.85f, 1.15f, .5f + .5f * Mathf.Cos(phase));
            float slow = Mathf.Sin(t * 1.2f + phase) * Mathf.Lerp(.7f, 3.2f, gust);
            float rustle = (Mathf.Sin(t * 7f + phase) + .35f * Mathf.Sin(t * 11.3f + phase * 2f))
                * Mathf.Lerp(.15f, 1.15f, gust);
            float angle = (slow + rustle) * variation * Mathf.SmoothStep(0, 1, (t - started) / 2f);
            transform.localRotation = rest * Quaternion.Euler(0, 0, angle);
        }
    }
}
