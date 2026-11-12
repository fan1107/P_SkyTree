using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SkyTree
{
    public sealed class VoyageResultMapView : MonoBehaviour
    {
        [SerializeField] private RectTransform[] points;
        [SerializeField] private RectTransform ship, destinationRing;
        [SerializeField] private Text positionLabel;
        [SerializeField] private RouteMapGraphic route;
        [SerializeField, Min(.05f)] private float secondsPerLeg = .65f;
        public bool IsTravelling { get; private set; }
        public int Destination { get; private set; }
        public Vector2 MarkerPosition => ship.anchoredPosition;
        public Vector2 Position(int index) => points[Mathf.Clamp(index, 0, points.Length - 1)].anchoredPosition;
        private static readonly Vector2 ShipOffset = new Vector2(0, 15);

        public void Show(VoyageProgress progress, Font font)
        {
            StopAllCoroutines(); IsTravelling = false;
            Destination = progress.ReachedPoint;
            positionLabel.font = font;
            destinationRing.anchoredPosition = Position(Destination);
            int reached = 0;
            for (int i = 1; i < progress.Route.Waypoints.Count; i++)
                if (progress.Route.Waypoints[i] == Destination) reached = i;
            var path = new Vector2[reached + 1];
            for (int i = 0; i < path.Length; i++) path[i] = Position(progress.Route.Waypoints[i]);
            route.SetRoute(path); route.gameObject.SetActive(path.Length > 1);
            Place(path[0]);
            StartCoroutine(Travel(path));
        }

        private void Place(Vector2 position)
        {
            ship.anchoredPosition = position + ShipOffset;
            positionLabel.rectTransform.anchoredPosition = position + new Vector2(0, -22);
        }

        private IEnumerator Travel(Vector2[] path)
        {
            IsTravelling = true; positionLabel.text = "항해 중";
            float started = Time.unscaledTime;
            float duration = (path.Length - 1) * secondsPerLeg;
            while (Time.unscaledTime - started < duration)
            {
                // One clock for the whole route preserves overshoot across legs on slow frames.
                float travelled = (Time.unscaledTime - started) / secondsPerLeg;
                int leg = Mathf.Min(Mathf.FloorToInt(travelled), path.Length - 2);
                Place(Vector2.Lerp(path[leg], path[leg + 1], Mathf.SmoothStep(0, 1, travelled - leg)));
                yield return null;
            }
            Place(Position(Destination)); positionLabel.text = "도착"; IsTravelling = false;
        }

        private void OnDisable() { StopAllCoroutines(); IsTravelling = false; }
    }
}
