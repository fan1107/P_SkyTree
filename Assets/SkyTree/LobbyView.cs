using System;
using UnityEngine;
using UnityEngine.UI;

namespace SkyTree
{
    public sealed class LobbyView : MonoBehaviour
    {
        [SerializeField] private LobbyCurrencyHud currency;
        [SerializeField] private ShipTreeGrowth tree;
        [SerializeField] private Button adventure;
        [SerializeField] private Button[] tabs;
        [SerializeField] private GameObject details;
        [SerializeField] private Text heading, description;
        [SerializeField] private Button close;
        private int selected = -1;
        private Vector2 shipOrigin;
        private Quaternion shipRotation;
        private float entered;

        public void Bind(Font font, int energy, int gold, int gems, Action depart)
        {
            foreach (var label in GetComponentsInChildren<Text>(true)) label.font = font;
            currency.Display(energy, gold, gems);
            adventure.onClick.AddListener(() => depart());
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                tabs[i].onClick.AddListener(() => SelectTab(index));
            }
            close.onClick.AddListener(CloseDetails);
            foreach (var addCurrency in currency.GetComponentsInChildren<Button>(true))
                addCurrency.onClick.AddListener(() => SelectTab(3));
            shipOrigin = ((RectTransform)tree.transform).anchoredPosition;
            shipRotation = tree.transform.localRotation;
            entered = Time.time;
        }

        private void Update()
        {
            var ship = (RectTransform)tree.transform;
            float t = Time.time - entered;
            float fade = Mathf.SmoothStep(0, 1, t / 2f);
            ship.anchoredPosition = shipOrigin + new Vector2(
                1.2f * Mathf.Sin(t * (2 * Mathf.PI / 12)),
                4.5f * Mathf.Sin(t * (2 * Mathf.PI / 8))) * fade;
            ship.localRotation = shipRotation * Quaternion.Euler(0, 0,
                .65f * Mathf.Sin(t * (2 * Mathf.PI / 10)) * fade);
        }

        private void SelectTab(int index)
        {
            if (selected == index) { CloseDetails(); return; }
            selected = index;
            heading.text = tabs[index].GetComponentInChildren<Text>().text;
            switch (index)
            {
                case 0: description.text = $"초기형 비행선   ·   하늘나무 {tree.Stage}단계"; break;
                case 1: description.text = "승선 동료   ·   선장 롱 · 여성 항해사"; break;
                case 2: description.text = "현재 진행 중인 뽑기가 없습니다."; break;
                default: description.text = "현재 판매 중인 상품이 없습니다."; break;
            }
            details.SetActive(true);
        }

        private void CloseDetails()
        {
            selected = -1;
            details.SetActive(false);
        }
    }
}
