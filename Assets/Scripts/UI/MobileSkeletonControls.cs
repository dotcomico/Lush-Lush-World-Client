using LushWorld.Building;
using LushWorld.Resource;
using LushWorld.Utilities;
using UnityEngine;

namespace LushWorld.UI
{
    // Attach to MobileSkeleton panel (parent of Delete + AddMterials buttons) inside PlayerRig.
    // Visible on mobile only, and only while a BlueprintInstance is within interaction range.
    // Delete button  → wire OnClick to OnDeletePressed()
    // AddMterials button → wire OnClick to OnAddMaterialsPressed()
    public class MobileSkeletonControls : MonoBehaviour
    {
        private CanvasGroup       _group;
        private BlueprintInstance _currentBp;

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

        private void OnEnable()  => ResourceInteractor.OnNearBlueprintChanged += HandleNearBlueprintChanged;
        private void OnDisable() => ResourceInteractor.OnNearBlueprintChanged -= HandleNearBlueprintChanged;

        private void HandleNearBlueprintChanged(BlueprintInstance bp)
        {
            _currentBp = bp;
            SetVisible(bp != null);
        }

        // Wire to AddMterials button OnClick in the Inspector.
        public void OnAddMaterialsPressed()
        {
            if (_currentBp == null) return;
            _currentBp.Interact(gameObject);
        }

        // Wire to Delete button OnClick in the Inspector.
        public void OnDeletePressed()
        {
            if (_currentBp == null) return;
            var bp = _currentBp;
            _currentBp = null;
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
