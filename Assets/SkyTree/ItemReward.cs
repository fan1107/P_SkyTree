using System;
using UnityEngine;

namespace SkyTree
{
    // This is the earned delta, never an inventory balance or a payout command.
    [Serializable]
    public struct ItemReward
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField] private long quantity;
        public ItemDefinition Item => item;
        public long Quantity => quantity;
        public bool IsValid => item && item.Icon && quantity > 0;
        public ItemReward(ItemDefinition item, long quantity) { this.item = item; this.quantity = quantity; }
    }
}
