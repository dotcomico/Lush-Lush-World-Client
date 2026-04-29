using System;
using LushWorld.Resource;
using LushWorld.World;
using TMPro;
using UnityEngine;

namespace LushWorld.Inventory
{
    // Player-owned MonoBehaviour. Single source of truth for inventory state.
    // All mutations go through Request*() methods — in Phase 2 these become [ServerRpc].
    // Static events let UI find the local player's inventory without a direct scene reference.
    //
    // Phase 2 upgrade: inherit NetworkBehaviour, Request*() → [ServerRpc(RequireOwnership = true)]
    public class InventorySystem : MonoBehaviour
    {
        public static event Action<InventoryData> OnInventoryReady;
        public static event Action OnInventoryDestroyed;
        public static event Action<bool> OnBackpackToggleRequested;

        // The locally-owned player's inventory. In Phase 2 (NGO), set only when IsOwner.
        public static InventorySystem LocalPlayer { get; private set; }

        public InventoryData Data { get; private set; }

        [SerializeField] private ItemRegistry _itemRegistry;

        [Header("Drop Settings")]
        [SerializeField] private float _dropMaxDistance = 2.5f;
        [SerializeField] private float _dropAngularDrag  = 5f;

        private bool _backpackOpen;

        private void Start()
        {
            LocalPlayer = this;
            Data = new InventoryData();
            OnInventoryReady?.Invoke(Data);
        }

        private void OnDestroy()
        {
            if (LocalPlayer == this) LocalPlayer = null;
            OnInventoryDestroyed?.Invoke();
        }

        // Global entry point for giving an item to the local player from anywhere in the codebase.
        public static bool GiveItem(string itemId, int quantity = 1)
        {
            if (LocalPlayer == null)
            {
                Debug.LogWarning("[Inventory] GiveItem called but no active player found.");
                return false;
            }
            return LocalPlayer.RequestAddItem(new ItemStack(itemId, quantity));
        }

        public bool RequestAddItem(ItemStack stack)
        {
            return Data.TryAddItem(stack, out _);
        }

        public int CountItem(string itemId)
        {
            int total = 0;
            for (int i = 0; i < InventoryData.HotbarSize; i++)
            {
                var s = Data.GetHotbarSlot(i);
                if (s.ItemId == itemId) total += s.Quantity;
            }
            for (int i = 0; i < InventoryData.BackpackSize; i++)
            {
                var s = Data.GetBackpackSlot(i);
                if (s.ItemId == itemId) total += s.Quantity;
            }
            return total;
        }

        public bool TryConsumeItem(string itemId, int qty = 1)
        {
            for (int i = 0; i < InventoryData.HotbarSize; i++)
            {
                var s = Data.GetHotbarSlot(i);
                if (s.ItemId == itemId && s.Quantity >= qty)
                {
                    Data.TryRemoveItem(i, true, qty);
                    return true;
                }
            }
            for (int i = 0; i < InventoryData.BackpackSize; i++)
            {
                var s = Data.GetBackpackSlot(i);
                if (s.ItemId == itemId && s.Quantity >= qty)
                {
                    Data.TryRemoveItem(i, false, qty);
                    return true;
                }
            }
            return false;
        }

        public void RequestRemoveItem(int slot, bool isHotbar, int qty)
        {
            Data.TryRemoveItem(slot, isHotbar, qty);
        }

        public void RequestSwapSlots(int fromIndex, bool fromHotbar, int toIndex, bool toHotbar)
        {
            var fromStack = Data.GetSlot(fromIndex, fromHotbar);
            var toStack   = Data.GetSlot(toIndex,   toHotbar);

            // Same item type: merge quantities up to MaxStackSize.
            // If dst is full, TryMergeIntoSlot returns false and we fall through to a normal swap.
            if (!fromStack.IsEmpty && !toStack.IsEmpty && fromStack.ItemId == toStack.ItemId)
            {
                var def      = _itemRegistry != null ? _itemRegistry.GetById(fromStack.ItemId) : null;
                int maxStack = def?.MaxStackSize ?? 64;
                if (Data.TryMergeIntoSlot(fromIndex, fromHotbar, toIndex, toHotbar, maxStack))
                    return;
            }

            Data.TrySwapSlots(fromIndex, fromHotbar, toIndex, toHotbar);
        }

        // Shift-click: moves the full stack at slotIndex to the other section (hotbar↔backpack).
        // Pass 1 merges into existing partial stacks; Pass 2 fills the first empty slot.
        public void RequestMoveToOtherSection(int slotIndex, bool isHotbar)
        {
            var srcStack = Data.GetSlot(slotIndex, isHotbar);
            if (srcStack.IsEmpty) return;

            bool dstHotbar = !isHotbar;
            int  dstSize   = dstHotbar ? InventoryData.HotbarSize : InventoryData.BackpackSize;

            var def      = _itemRegistry != null ? _itemRegistry.GetById(srcStack.ItemId) : null;
            int maxStack = def?.MaxStackSize ?? 64;

            // Pass 1: pour into existing partial stacks of the same item type.
            for (int i = 0; i < dstSize; i++)
            {
                var dstStack = Data.GetSlot(i, dstHotbar);
                if (dstStack.IsEmpty || dstStack.ItemId != srcStack.ItemId) continue;
                Data.TryMergeIntoSlot(slotIndex, isHotbar, i, dstHotbar, maxStack);
                srcStack = Data.GetSlot(slotIndex, isHotbar);
                if (srcStack.IsEmpty) return;
            }

            // Pass 2: place whatever remains in the first empty slot.
            for (int i = 0; i < dstSize; i++)
            {
                if (!Data.GetSlot(i, dstHotbar).IsEmpty) continue;
                Data.TrySwapSlots(slotIndex, isHotbar, i, dstHotbar);
                return;
            }
        }

        public void RequestSplitStack(int slotIndex, bool isHotbar)
        {
            Data.TrySplitStack(slotIndex, isHotbar, out _);
        }

        public void RequestSelectSlot(int index)
        {
            Data.SetSelectedHotbarSlot(index);
        }

        public void RequestCycleSlot(int direction)
        {
            Data.CycleHotbarSelection(direction);
        }

        public void RequestToggleBackpack()
        {
            _backpackOpen = !_backpackOpen;
            OnBackpackToggleRequested?.Invoke(_backpackOpen);
        }

        // Q key — always drops exactly 1 unit, leaving the rest in the slot.
        public void RequestDropActiveItem()
        {
            DropFromSlot(Data.SelectedHotbarSlot, isHotbar: true, quantity: 1);
        }

        // Drag-to-world — drops the full stack as a single world object.
        public void RequestDropSlotItem(int slotIndex, bool isHotbar)
        {
            ItemStack stack = Data.GetSlot(slotIndex, isHotbar);
            if (!stack.IsEmpty)
                DropFromSlot(slotIndex, isHotbar, quantity: stack.Quantity);
        }

        // Death — scatters every droppable item in a random ring around the player.
        public void RequestDeathScatterDrop()
        {
            for (int i = 0; i < InventoryData.HotbarSize; i++)
                ScatterDropFromSlot(i, true);
            for (int i = 0; i < InventoryData.BackpackSize; i++)
                ScatterDropFromSlot(i, false);
        }

        private void ScatterDropFromSlot(int slotIndex, bool isHotbar)
        {
            ItemStack stack = Data.GetSlot(slotIndex, isHotbar);
            if (stack.IsEmpty) return;
            if (_itemRegistry == null || !_itemRegistry.TryGetById(stack.ItemId, out ItemDefinition def)) return;
            if (!def.IsDroppable || def.WorldPrefab == null) return;

            int toDrop = stack.Quantity;
            Data.TryRemoveItem(slotIndex, isHotbar, toDrop);

            Vector2 rand2D = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(1f, 3f);
            Vector3 spawnPos = transform.position + new Vector3(rand2D.x, 0.5f, rand2D.y);

            var spawned = UnityEngine.Object.Instantiate((UnityEngine.Object)def.WorldPrefab, spawnPos, UnityEngine.Random.rotation);
            GameObject dropped = spawned as GameObject ?? (spawned as Component)?.gameObject;
            if (dropped == null) return;

            if (dropped.TryGetComponent(out ResourceNode node))
                node.SetQuantity(toDrop);

            foreach (var mc in dropped.GetComponentsInChildren<MeshCollider>())
                mc.convex = true;

            if (!dropped.TryGetComponent(out Rigidbody rb))
                rb = dropped.AddComponent<Rigidbody>();
            rb.mass           = 1f;
            rb.linearDamping  = 0.5f;
            rb.angularDamping = _dropAngularDrag;

            Vector3 burst = new Vector3(rand2D.x, 1.5f, rand2D.y).normalized;
            rb.AddForce(burst * UnityEngine.Random.Range(2f, 5f), ForceMode.Impulse);

            SpawnQuantityLabel(dropped, toDrop);
        }

        private void DropFromSlot(int slotIndex, bool isHotbar, int quantity)
        {
            ItemStack stack = Data.GetSlot(slotIndex, isHotbar);
            if (stack.IsEmpty) return;
            if (_itemRegistry == null || !_itemRegistry.TryGetById(stack.ItemId, out ItemDefinition def)) return;
            if (!def.IsDroppable || def.WorldPrefab == null) return;

            int toDrop = Mathf.Min(quantity, stack.Quantity);
            Data.TryRemoveItem(slotIndex, isHotbar, toDrop);

            Vector3 spawnPos = GetDropSpawnPosition();
            var spawned = UnityEngine.Object.Instantiate((UnityEngine.Object)def.WorldPrefab, spawnPos, UnityEngine.Random.rotation);
            GameObject dropped = spawned as GameObject ?? (spawned as Component)?.gameObject;
            if (dropped == null) return;

            if (dropped.TryGetComponent(out ResourceNode node))
                node.SetQuantity(toDrop);

            // Concave MeshColliders are incompatible with dynamic Rigidbodies — fix before adding.
            foreach (var mc in dropped.GetComponentsInChildren<MeshCollider>())
                mc.convex = true;

            // Add physics only on the dropped clone; the original prefab stays static for terrain spawns.
            if (!dropped.TryGetComponent(out Rigidbody rb))
                rb = dropped.AddComponent<Rigidbody>();
            rb.mass           = 1f;
            rb.linearDamping  = 0.5f;
            rb.angularDamping = _dropAngularDrag;
            Vector3 throwDir  = transform.forward + Vector3.up * 0.2f;
            rb.AddForce(throwDir.normalized * 2f, ForceMode.Impulse);

            SpawnQuantityLabel(dropped, toDrop);
        }

        private void SpawnQuantityLabel(GameObject parent, int quantity)
        {
            if (quantity <= 1) return;

            var go = new GameObject("QuantityLabel");
            go.transform.SetParent(parent.transform, false);

            // Neutralize parent world scale so font renders at intended size regardless of prefab scale.
            Vector3 ps = parent.transform.lossyScale;
            go.transform.localScale = new Vector3(1f / ps.x, 1f / ps.y, 1f / ps.z);

            // Sit the label just above the highest renderer bound — works for any prefab size.
            float topY = parent.transform.position.y;
            foreach (var r in parent.GetComponentsInChildren<Renderer>())
                topY = Mathf.Max(topY, r.bounds.max.y);
            go.transform.position = new Vector3(
                parent.transform.position.x,
                topY + 0.12f,
                parent.transform.position.z);

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text         = quantity.ToString();
            tmp.fontSize     = 2.5f;
            tmp.fontStyle    = FontStyles.Bold;
            tmp.alignment    = TextAlignmentOptions.Center;
            tmp.color        = Color.white;
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = Color.black;

            go.AddComponent<ItemDropLabel>();
        }

        private Vector3 GetDropSpawnPosition()
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam != null)
            {
#if ENABLE_INPUT_SYSTEM
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse != null)
                {
                    Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
                    if (Physics.Raycast(ray, out RaycastHit hit, 20f))
                        return ClampDropPoint(hit.point + Vector3.up * 0.1f);
                }
#else
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 20f))
                    return ClampDropPoint(hit.point + Vector3.up * 0.1f);
#endif
            }
            return transform.position + transform.forward * Mathf.Min(1.5f, _dropMaxDistance) + Vector3.up * 0.3f;
        }

        // Clamps the drop point to _dropMaxDistance on the XZ plane so items never land far away.
        private Vector3 ClampDropPoint(Vector3 point)
        {
            Vector3 toPoint   = point - transform.position;
            Vector3 toPointXZ = new Vector3(toPoint.x, 0f, toPoint.z);
            if (toPointXZ.magnitude > _dropMaxDistance)
                point = transform.position + toPointXZ.normalized * _dropMaxDistance + Vector3.up * 0.3f;
            return point;
        }
    }
}
