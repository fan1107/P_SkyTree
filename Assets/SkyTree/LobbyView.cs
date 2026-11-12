using System;
using UnityEngine;
using UnityEngine.UI;

namespace SkyTree
{
    public sealed class LobbyView : MonoBehaviour
    {
        [SerializeField] private LobbyCurrencyHud currency;
        [SerializeField] private Button adventure;
        [SerializeField] private Button[] tabs;
        [SerializeField] private GameObject details;
        [SerializeField] private Text heading, description;
        [SerializeField] private Text worldLabel;
        [SerializeField] private Text locationLabel;
        [SerializeField] private Button close;
        private int selected = -1;
        private Func<int> treeStage;

        // UI는 전달받은 데이터와 콜백만 표시하며 배와 배경의 Transform을 소유하지 않는다.
        public void Bind(Font font, int energy, int gold, int gems, Action depart, int worldNumber,
            string location, Color worldColor, Func<int> getTreeStage)
        {
            foreach (var label in GetComponentsInChildren<Text>(true)) label.font = font;
            if (worldLabel) worldLabel.text = $"WORLD {worldNumber:00}";
            worldLabel.color = worldColor;
            locationLabel.text = location;
            treeStage = getTreeStage;
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
        }

        private void SelectTab(int index)
        {
            if (selected == index) { CloseDetails(); return; }
            selected = index;
            heading.text = tabs[index].GetComponentInChildren<Text>().text;
            switch (index)
            {
                case 0: description.text = $"초기형 비행선   ·   하늘나무 {treeStage()}단계"; break;
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
