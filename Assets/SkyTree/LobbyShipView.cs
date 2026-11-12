using UnityEngine;

namespace SkyTree
{
    // 로비용 배의 표시와 부유 연출을 관리한다. 전투용 배에는 붙이지 않는다.
    public sealed class LobbyShipView : MonoBehaviour
    {
        [SerializeField] private ShipTreeGrowth tree;
        [SerializeField] private Vector2 floatDistance = new Vector2(1.2f, 4.5f);
        [SerializeField] private Vector2 floatPeriod = new Vector2(12, 8);
        [SerializeField] private float tiltDegrees = .65f;
        [SerializeField] private float tiltPeriod = 10;
        private RectTransform rect;
        private Vector2 origin;
        private Quaternion rotation;
        private float entered;
        private bool placed;
        public int TreeStage => tree.Stage;

        // 같은 로비 좌표계의 앵커를 복사한다. 연출은 이 배에만 적용되어 배경이 흔들리지 않는다.
        public void Place(RectTransform anchor)
        {
            rect = (RectTransform)transform;
            rect.anchorMin = anchor.anchorMin; rect.anchorMax = anchor.anchorMax;
            rect.pivot = anchor.pivot; rect.sizeDelta = anchor.sizeDelta;
            rect.anchoredPosition3D = anchor.anchoredPosition3D;
            rect.localRotation = anchor.localRotation; rect.localScale = anchor.localScale;
            origin = rect.anchoredPosition; rotation = rect.localRotation;
            entered = Time.time; placed = true;
        }

        private void Update()
        {
            if (!placed) return;
            float t = Time.time - entered;
            float fade = Mathf.SmoothStep(0, 1, t / 2f);
            rect.anchoredPosition = origin + new Vector2(
                floatDistance.x * Mathf.Sin(t * 2 * Mathf.PI / Mathf.Max(.01f, floatPeriod.x)),
                floatDistance.y * Mathf.Sin(t * 2 * Mathf.PI / Mathf.Max(.01f, floatPeriod.y))) * fade;
            rect.localRotation = rotation * Quaternion.Euler(0, 0,
                tiltDegrees * Mathf.Sin(t * 2 * Mathf.PI / Mathf.Max(.01f, tiltPeriod)) * fade);
        }
    }
}
