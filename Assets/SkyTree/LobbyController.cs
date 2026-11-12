using System;
using UnityEngine;

namespace SkyTree
{
    // 로비 수명 동안 공통 UI, 지역 배경, 배를 한 번씩 조합한다.
    public sealed class LobbyController : MonoBehaviour
    {
        [SerializeField] private RectTransform backgroundRoot;
        [SerializeField] private RectTransform shipRoot;
        [SerializeField] private RectTransform foregroundRoot;
        [SerializeField] private RectTransform popupRoot;
        [SerializeField] private LobbyView ui;
        [SerializeField] private LobbyShipView shipPrefab;
        public RectTransform PopupRoot => popupRoot;
        public LobbyBackgroundView Background { get; private set; }
        public LobbyShipView Ship { get; private set; }

        // 지역은 배경만 결정한다. 주인공 배는 별도로 연결된 배 프리팹을 사용한다.
        public void Open(WorldLobbyTable.Region region, Font font, int energy, int gold, int gems,
            Action depart, int worldNumber)
        {
            if (Background || Ship) throw new InvalidOperationException("이미 구성된 로비입니다.");
            if (region == null || !region.Background || !region.Background.ShipAnchor ||
                !backgroundRoot || !shipRoot || !foregroundRoot || !popupRoot || !ui || !shipPrefab)
                throw new InvalidOperationException("로비 UI·배경·배·배치 기준 연결을 확인하세요: " + name);
            Background = Instantiate(region.Background, backgroundRoot, false);
            Ship = Instantiate(shipPrefab, shipRoot, false);
            Ship.Place(Background.ShipAnchor);
            Background.AttachForeground(foregroundRoot);
            ui.Bind(font, energy, gold, gems, depart, worldNumber, region.DisplayName,
                region.WorldLabelColor, () => Ship.TreeStage);
        }
    }
}
