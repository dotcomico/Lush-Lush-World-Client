using UnityEngine;
using TMPro;
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
        [SerializeField] private float pickupRadius = 2.5f;
        [SerializeField] private TMP_Text interactPrompt;
        [SerializeField] private ItemRegistry itemRegistry;
        [SerializeField] private GameObject mobilePickupButton;

        private ResourceNode  _nearestNode;
        private IInteractable _nearestInteractable;
        private readonly Collider[] _overlapBuffer = new Collider[16];

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
                _nearestNode         = newNode;
                _nearestInteractable = newInteractable;
                UpdatePrompt();
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

            if (_nearestInteractable != null)
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
