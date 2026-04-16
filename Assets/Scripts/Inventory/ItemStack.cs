using System;

namespace LushWorld.Inventory
{
    // Value type so it copies cleanly into NetworkVariable<ItemStack> in Phase 2.
    // Phase 2 upgrade: add INetworkSerializable, implement NetworkSerialize<T>.
    [Serializable]
    public struct ItemStack
    {
        public static readonly ItemStack Empty = new ItemStack(string.Empty, 0);

        public string ItemId;
        public int Quantity;

        public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Quantity <= 0;

        public ItemStack(string itemId, int quantity)
        {
            ItemId = itemId;
            Quantity = quantity;
        }
    }
}
