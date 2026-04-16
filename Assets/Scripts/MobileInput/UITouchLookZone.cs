using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

/// <summary>
/// Trackpad-style look control for mobile: outputs the frame-to-frame screen-pixel
/// delta while the finger is dragging, and outputs Vector2.zero when the finger lifts.
///
/// Unlike UIVirtualJoystick / UIVirtualTouchZone, holding your finger still produces
/// zero output — the camera stops rotating immediately, with no drift.
///
/// Usage:
///   1. Attach to a full-stretch transparent panel covering the right side of the screen.
///   2. Wire onLookDelta → UICanvasControllerInput.VirtualLookInput.
///   3. Tune 'sensitivity' in the Inspector (start at 0.2; also see controller sensitivity fields).
/// </summary>
public class UITouchLookZone : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [System.Serializable]
    public class LookEvent : UnityEvent<Vector2> { }

    [Header("Settings")]
    [Tooltip("Scales the raw pixel delta sent to the look input. " +
             "Lower = slower camera. Tune alongside RotationSpeed / HorizontalSensitivity.")]
    public float sensitivity = 0.2f;

    [Tooltip("Invert the horizontal (X) look axis.")]
    public bool invertX;

    [Tooltip("Invert the vertical (Y) look axis. " +
             "Matches the existing look joystick's invertYOutputValue = true setting.")]
    public bool invertY = true;

    [Header("Output")]
    public LookEvent onLookDelta;

    // Screen-space position reported in the previous drag event.
    private Vector2 _prevPosition;

    public void OnPointerDown(PointerEventData eventData)
    {
        _prevPosition = eventData.position;
        // Immediately zero out any stale look vector from the previous touch.
        onLookDelta.Invoke(Vector2.zero);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.position - _prevPosition;
        _prevPosition = eventData.position;

        if (invertX) delta.x = -delta.x;
        if (invertY) delta.y = -delta.y;

        onLookDelta.Invoke(delta * sensitivity);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _prevPosition = Vector2.zero;
        onLookDelta.Invoke(Vector2.zero);
    }
}
