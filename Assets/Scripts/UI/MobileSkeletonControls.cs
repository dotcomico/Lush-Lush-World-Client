using LushWorld.Building;
using LushWorld.Resource;
using LushWorld.Utilities;
using UnityEngine;

namespace LushWorld.UI
{
    // Attach to MobileSkeleton panel (parent of Delete + AddMterials buttons) inside PlayerRig.
    // Visible on mobile only, and only while an IInteractable is within interaction range.
    // Works for both BlueprintInstance (building skeleton) and HeartStone (place hearts).
    // Delete button  → wire OnClick to OnDeletePressed()  (auto-hidden for non-blueprint interactables)
    // AddMterials button → wire OnClick to OnAddMaterialsPressed()
    // Inspector: assign _deleteButton → the Delete button GameObject so it can be toggled.
    public class MobileSkeletonControls : MonoBehaviour
    {
        [SerializeField] private GameObject _deleteButton;

        private CanvasGroup   _group;
        private IInteractable _currentInteractable;

        private void Awake()
        {
            if (!PlatformDetector.IsMobile)
            {
                gameObject.SetActive(false);
                return;
            }

            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();

            SetVisible(false);
        }

        private void OnEnable()  => ResourceInteractor.OnNearInteractableChanged += HandleNearInteractableChanged;
        private void OnDisable() => ResourceInteractor.OnNearInteractableChanged -= HandleNearInteractableChanged;

        private void HandleNearInteractableChanged(IInteractable interactable)
        {
            _currentInteractable = interactable;
            bool show = interactable != null;
            SetVisible(show);

            // Delete is only meaningful for building skeletons, not for the Heartstone.
            if (_deleteButton != null)
                _deleteButton.SetActive(show && interactable is BlueprintInstance);
        }

        // Wire to AddMterials button OnClick in the Inspector.
        public void OnAddMaterialsPressed()
        {
            _currentInteractable?.Interact(gameObject);
        }

        // Wire to Delete button OnClick in the Inspector.
        public void OnDeletePressed()
        {
            if (_currentInteractable is not BlueprintInstance bp) return;
            _currentInteractable = null;
            SetVisible(false);
            bp.Demolish();
        }

        private void SetVisible(bool show)
        {
            if (_group == null) return;
            _group.alpha          = show ? 1f : 0f;
            _group.interactable   = show;
            _group.blocksRaycasts = show;
        }
    }
}
