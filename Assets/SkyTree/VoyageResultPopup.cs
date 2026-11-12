using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkyTree
{
    public sealed class VoyageResultPopup : MonoBehaviour
    {
        [SerializeField] private VoyageResultMapView map;
        [SerializeField] private UIItemSlot itemSlotPrefab;
        [SerializeField] private RectTransform rewardContent;
        [SerializeField] private ScrollRect rewardScroll;
        [SerializeField] private GameObject emptyRewards;
        [SerializeField] private Button confirm;
        private readonly List<UIItemSlot> slots = new List<UIItemSlot>();
        private Action onConfirm;
        private bool confirmed;
        public VoyageResultMapView Map => map;
        public int VisibleRewardCount { get; private set; }

        private void Awake() => confirm.onClick.AddListener(Confirm);

        // The caller supplies already-earned rewards. This view never grants currency.
        public void Open(Font font, VoyageProgress progress, IReadOnlyList<ItemReward> rewards, Action callback = null)
        {
            if (progress == null || progress.Phase != VoyagePhase.Victory)
                throw new ArgumentException("항해 성공 팝업에는 완료된 승리 결과가 필요합니다.", nameof(progress));
            gameObject.SetActive(true); transform.SetAsLastSibling();
            confirmed = false; onConfirm = callback; confirm.interactable = true;
            foreach (var label in GetComponentsInChildren<Text>(true)) label.font = font;
            BindRewards(rewards, font);
            map.Show(progress, font);
        }

        public void BindRewards(IReadOnlyList<ItemReward> rewards, Font font)
        {
            VisibleRewardCount = 0;
            if (rewards != null)
                foreach (var reward in rewards)
                {
                    if (!reward.IsValid) continue;
                    if (VisibleRewardCount == slots.Count) slots.Add(Instantiate(itemSlotPrefab, rewardContent, false));
                    var slot = slots[VisibleRewardCount++];
                    slot.gameObject.SetActive(true); slot.Bind(reward, font);
                }
            for (int i = VisibleRewardCount; i < slots.Count; i++)
            { slots[i].Clear(); slots[i].gameObject.SetActive(false); }
            emptyRewards.SetActive(VisibleRewardCount == 0);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rewardContent);
            rewardScroll.StopMovement(); rewardScroll.verticalNormalizedPosition = 1;
        }

        public void Confirm()
        {
            if (confirmed) return;
            confirmed = true; confirm.interactable = false;
            var callback = onConfirm; onConfirm = null;
            gameObject.SetActive(false); callback?.Invoke();
        }

        private void OnDestroy() => confirm.onClick.RemoveListener(Confirm);
    }
}
