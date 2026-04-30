using System;
using UnityEngine;
using LushWorld.Inventory;
using LushWorld.Utilities;

namespace LushWorld.World
{
    // Place on the Heartstone prefab. Implements IInteractable so ResourceInteractor
    // detects it via OverlapSphere and routes E-key presses to Interact().
    // Requires a SphereCollider (IsTrigger = true, radius ~2.5) on the same GameObject.
    public class HeartStone : MonoBehaviour, IInteractable
    {
        public static HeartStone Instance { get; private set; }
        public static event Action OnAllHeartsPlaced;
        public static event Action OnHeartsChanged;

        public int  PlacedCount => _placedCount;
        public bool IsComplete  => _placedCount >= _totalHearts;

        [SerializeField] private int _totalHearts = 6;
        [SerializeField] private GameObject[] _heartSlots;

        private int _placedCount;
        private const string HeartItemId = "heart";

        private void Awake()
        {
            Instance = this;
            foreach (var slot in _heartSlots)
                if (slot != null) slot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public string InteractionLabel
        {
            get
            {
                if (_placedCount >= _totalHearts)
                    return "All hearts placed!";

                var inv = InventorySystem.LocalPlayer;
                int carried = inv != null ? inv.CountItem(HeartItemId) : 0;

                if (carried == 0)
                    return $"Bring hearts here\n{_placedCount}/{_totalHearts}";

                return $"[E] Place heart ({carried} carried)\n{_placedCount}/{_totalHearts}";
            }
        }

        public void Interact(GameObject player)
        {
            if (_placedCount >= _totalHearts) return;

            var inv = InventorySystem.LocalPlayer;
            if (inv == null || !inv.TryConsumeItem(HeartItemId)) return;

            if (_placedCount < _heartSlots.Length && _heartSlots[_placedCount] != null)
                _heartSlots[_placedCount].SetActive(true);

            _placedCount++;
            OnHeartsChanged?.Invoke();

            if (_placedCount >= _totalHearts)
            {
                Debug.Log("[HeartStone] All hearts placed — game complete!");
                OnAllHeartsPlaced?.Invoke();
            }
        }

        // Restores heart count from a save file; activates the correct heart slot displays.
        public void LoadHearts(int count)
        {
            _placedCount = Mathf.Clamp(count, 0, _totalHearts);
            for (int i = 0; i < _heartSlots.Length; i++)
            {
                if (_heartSlots[i] != null)
                    _heartSlots[i].SetActive(i < _placedCount);
            }
        }
    }
}
