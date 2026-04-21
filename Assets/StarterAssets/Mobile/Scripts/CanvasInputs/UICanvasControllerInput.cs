using UnityEngine;

namespace StarterAssets
{
    public class UICanvasControllerInput : MonoBehaviour
    {

        [Header("Output")]
        public StarterAssetsInputs starterAssetsInputs;

        private UIVirtualButton _slideButton;

        private void Start()
        {
            // Auto-wire: find the slide button within this canvas at startup (one-time, not hot path)
            foreach (var btn in GetComponentsInChildren<UIVirtualButton>(includeInactive: true))
            {
                if (btn.gameObject.name == "UI_Virtual_Button_Slide")
                {
                    _slideButton = btn;
                    _slideButton.buttonStateOutputEvent.AddListener(VirtualSlideInput);
                    break;
                }
            }
        }

        private void OnDestroy()
        {
            if (_slideButton != null)
                _slideButton.buttonStateOutputEvent.RemoveListener(VirtualSlideInput);
        }

        public void VirtualMoveInput(Vector2 virtualMoveDirection)
        {
            starterAssetsInputs.MoveInput(virtualMoveDirection);
        }

        public void VirtualLookInput(Vector2 virtualLookDirection)
        {
            starterAssetsInputs.LookInput(virtualLookDirection);
        }

        public void VirtualJumpInput(bool virtualJumpState)
        {
            starterAssetsInputs.JumpInput(virtualJumpState);
        }

        public void VirtualSprintInput(bool virtualSprintState)
        {
            starterAssetsInputs.SprintInput(virtualSprintState);
        }

        public void VirtualSlideInput(bool virtualSlideState)
        {
            starterAssetsInputs.SlideInput(virtualSlideState);
        }

    }

}
