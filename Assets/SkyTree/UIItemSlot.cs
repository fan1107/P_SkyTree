using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace SkyTree
{
    public sealed class UIItemSlot : MonoBehaviour
    {
        [SerializeField] private Image itemIcon, gradeBackground;
        [SerializeField] private Text countText;
        [SerializeField] private Button clickArea;
        private Action<ItemDefinition> onClick;
        public ItemDefinition Item { get; private set; }
        public long Quantity { get; private set; }

        private void Awake() => clickArea.onClick.AddListener(Click);

        public void Bind(ItemReward reward, Font font, Action<ItemDefinition> callback = null)
        {
            Clear();
            if (!reward.IsValid) return;
            Item = reward.Item; Quantity = reward.Quantity; onClick = callback;
            itemIcon.sprite = Item.Icon; itemIcon.enabled = true;
            gradeBackground.color = Item.FrameTint;
            countText.font = font;
            countText.text = "×" + Quantity.ToString("N0", CultureInfo.InvariantCulture);
            clickArea.interactable = callback != null;
        }

        public void Clear()
        {
            onClick = null; Item = null; Quantity = 0;
            itemIcon.sprite = null; itemIcon.enabled = false;
            countText.text = ""; gradeBackground.color = Color.white;
            clickArea.interactable = false;
        }

        private void Click() { if (Item) onClick?.Invoke(Item); }
        private void OnDestroy() => clickArea.onClick.RemoveListener(Click);
    }
}
