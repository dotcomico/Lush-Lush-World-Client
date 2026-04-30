using System;
using System.Text;
using UnityEngine;
using TMPro;
using LushWorld.Building;
using LushWorld.Inventory;
using LushWorld.Utilities;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LushWorld.Resource
{
    // Lives on PlayerRig root alongside InventoryInputHandler.
    // Uses the same PlayerInput SendMessage pattern — Unity calls OnInteract(InputValue) automatically.
    // Phase 2 upgrade: early-return if !IsOwner so non-owned players don't process input.
    public class ResourceInteractor : MonoBehaviour
    {
        // Fires whenever the nearest BlueprintInstance changes (null = left range, non-null = entered range).
        // Consumed by MobileSkeletonControls to show/hide skeleton action buttons on mobile.
        public static event Action<BlueprintInstance> OnNearBlueprintChanged;

        // Fires whenever the nearest IInteractable changes (covers BlueprintInstance, HeartStone, and any future interactable).
        public static event Action<IInteractable> OnNearInteractableChanged;

        [SerializeField] private float pickupRadius = 2.5f;
        [SerializeField] private TMP_Text interactPrompt;
        [SerializeField] private ItemRegistry itemRegistry;
        [SerializeField] private GameObject mobilePickupButton;

        private ResourceNode  _nearestNode;
        private IInteractable _nearestInteractable;
        private readonly Collider[] _overlapBuffer = new Collider[16];
        private float _rHoldTime;
        private const float DemolishHoldDuration = 1.5f;
        private readonly StringBuilder _promptBuilder = new();

        private static bool IsMobile() => Application.isMobilePlatform;

        private void Start()
        {
            if (interactPrompt != null)
                interactPrompt.gameObject.SetActive(false);
            if (mobilePickupButton != null)
                mobilePickupButton.SetActive(false);
        }

        private void Update()
        {
            FindNearestNode();
            HandleBlueprintRHold();
        }

        private void HandleBlueprintRHold()
        {
            if (_nearestInteractable is not BlueprintInstance bp || bp == null)
            {
                _rHoldTime = 0f;
                return;
            }

#if ENABLE_INPUT_SYSTEM
            bool ePressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
            bool rHeld    = Keyboard.current != null && Keyboard.current.rKey.isPressed;
#else
            bool ePressed = Input.GetKeyDown(KeyCode.E);
            bool rHeld    = Input.GetKey(KeyCode.R);
#endif
            // E: deposit one material directly (prompt refreshes immediately after)
            if (ePressed)
            {
                TryPickupNearest();
                return;
            }

            // R hold: demolish after 1.5 s
            if (!rHeld) { _rHoldTime = 0f; return; }
            _rHoldTime += Time.deltaTime;
            if (_rHoldTime >= DemolishHoldDuration)
            {
                _rHoldTime = 0f;
                bp.Demolish();
                _nearestInteractable = null;
                UpdatePrompt();
            }
        }

        private void FindNearestNode()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, pickupRadius, _overlapBuffer);

            ResourceNode  bestNode         = null;
            float         bestNodeDist     = float.MaxValue;
            IInteractable bestInteractable = null;
            float         bestInteractDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var node = _overlapBuffer[i].GetComponentInParent<ResourceNode>();
                if (node != null)
                {
                    float d = Vector3.SqrMagnitude(node.transform.position - transform.position);
                    if (d < bestNodeDist) { bestNodeDist = d; bestNode = node; }
                }

                var interactable = _overlapBuffer[i].GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    float d = Vector3.SqrMagnitude(_overlapBuffer[i].transform.position - transform.position);
                    if (d < bestInteractDist) { bestInteractDist = d; bestInteractable = interactable; }
                }
            }

            // IInteractable wins on tie; both null → show nothing.
            bool interactableWins = bestInteractable != null &&
                                    (bestNode == null || bestInteractDist <= bestNodeDist);

            ResourceNode  newNode         = interactableWins ? null : bestNode;
            IInteractable newInteractable = interactableWins ? bestInteractable : null;

            if (newNode != _nearestNode || newInteractable != _nearestInteractable)
            {
                var oldBp           = _nearestInteractable as BlueprintInstance;
                var newBp           = newInteractable      as BlueprintInstance;
                var oldInteractable = _nearestInteractable;

                _nearestNode         = newNode;
                _nearestInteractable = newInteractable;
                UpdatePrompt();

                if (oldBp != newBp)
                    OnNearBlueprintChanged?.Invoke(newBp);

                if (oldInteractable != newInteractable)
                    OnNearInteractableChanged?.Invoke(newInteractable);
            }
        }

        private void UpdatePrompt()
        {
            if (IsMobile())
            {
                if (mobilePickupButton != null)
                    mobilePickupButton.SetActive(_nearestNode != null);
                if (interactPrompt != null)
                    interactPrompt.gameObject.SetActive(false);
                return;
            }

            if (interactPrompt == null) return;

            if (_nearestInteractable is BlueprintInstance bp && bp != null)
            {
                _promptBuilder.Clear();
                foreach (var ing in bp.Def.Cost)
                {
                    string name = ing.ItemId;
                    if (itemRegistry != null && itemRegistry.TryGetById(ing.ItemId, out var itemDef))
                        name = itemDef.DisplayName;
                    int have = bp.GetDeposited(ing.ItemId);
                    _promptBuilder.AppendLine($"{name}: {have}/{ing.Quantity}");
                }
                _promptBuilder.Append("[E] Add Material   [Hold R] Remove");
                interactPrompt.text = _promptBuilder.ToString();
                interactPrompt.gameObject.SetActive(true);
            }
            else if (_nearestInteractable != null)
            {
                interactPrompt.text = _nearestInteractable.InteractionLabel;
                interactPrompt.gameObject.SetActive(true);
            }
            else if (_nearestNode != null)
            {
                string displayName = _nearestNode.ItemId;
                if (itemRegistry != null && itemRegistry.TryGetById(_nearestNode.ItemId, out var def))
                    displayName = def.DisplayName;
                interactPrompt.text = $"Press E to pick up {displayName}";
                interactPrompt.gameObject.SetActive(true);
            }
            else
            {
                interactPrompt.gameObject.SetActive(false);
            }
        }

        // Called by the mobile pickup button via Button.OnClick, and by OnInteract on desktop.
        public void TryPickupNearest()
        {
            if (_nearestInteractable != null)
            {
                _nearestInteractable.Interact(gameObject);
                UpdatePrompt();
                return;
            }
            if (_nearestNode == null) return;
            _nearestNode.TryPickup();
            _nearestNode = null;
            UpdatePrompt();
        }

#if ENABLE_INPUT_SYSTEM
        public void OnInteract(InputValue value)
        {
            if (!value.isPressed) return;
            TryPickupNearest();
        }
#endif
    }
}
