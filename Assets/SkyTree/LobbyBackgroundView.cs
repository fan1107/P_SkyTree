using UnityEngine;

namespace SkyTree
{
    // 지역의 그림·구름·빛과 배 배치 기준을 소유한다. 배 외형과 UI는 포함하지 않는다.
    public sealed class LobbyBackgroundView : MonoBehaviour
    {
        [SerializeField] private RectTransform shipAnchor;
        [SerializeField] private RectTransform foreground;
        public RectTransform ShipAnchor => shipAnchor;

        // 빛은 기존 화면처럼 배 위, UI 아래에 그린다. 로비 루트가 함께 정리한다.
        public void AttachForeground(RectTransform parent)
        {
            if (foreground) foreground.SetParent(parent, false);
        }
    }
}
