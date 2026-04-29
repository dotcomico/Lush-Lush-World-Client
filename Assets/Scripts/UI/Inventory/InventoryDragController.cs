using UnityEngine;
using UnityEngine.UI;

namespace LushWorld.UI.Inventory
{
    // Singleton that owns the floating drag icon and all drag-in-flight state.
    // Attach to the InventoryUI root inside PlayerRig.prefab — requires a Canvas on the same GameObject.
    // Fully self-wiring: creates its own DragIcon Image at runtime, no Inspector setup needed.
    public class InventoryDragController : MonoBehaviour
    {
        public static InventoryDragController Instance { get; private set; }

        public static event System.Action OnDragBegan;
        public static event System.Action OnDragEnded;

        public bool IsDragging { get; private set; }
        public int DragSourceIndex { get; private set; }
        public bool DragSourceIsHotbar { get; private set; }

        private Canvas _canvas;
        private RectTransform _canvasRect;
        private Image _dragIconImage;
        private RectTransform _dragIconRect;

        private void Awake()
        {
            Instance = this;
            _canvas = GetComponent<Canvas>();
            _canvasRect = GetComponent<RectTransform>();
            SpawnDragIcon();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // Creates the floating icon as the last child of the Canvas so it renders above all slots.
        private void SpawnDragIcon()
        {
            var go = new GameObject("DragIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            go.layer = gameObject.layer;

            _dragIconRect = go.GetComponent<RectTransform>();
            _dragIconRect.sizeDelta = new Vector2(64f, 64f);
            _dragIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            _dragIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            _dragIconRect.pivot = new Vector2(0.5f, 0.5f);

            _dragIconImage = go.GetComponent<Image>();
            _dragIconImage.raycastTarget = false;
            _dragIconImage.preserveAspect = true;

            go.SetActive(false);
        }

        public void BeginDrag(int slotIndex, bool isHotbar, Sprite icon)
        {
            IsDragging = true;
            DragSourceIndex = slotIndex;
            DragSourceIsHotbar = isHotbar;

            _dragIconImage.sprite = icon;
            _dragIconImage.color = icon != null ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
            _dragIconImage.gameObject.SetActive(true);

            OnDragBegan?.Invoke();
        }

        public void UpdateDragPosition(Vector2 screenPos)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPos, _canvas.worldCamera, out var localPoint))
            {
                _dragIconRect.localPosition = localPoint;
            }
        }

        // Called by the drop target to atomically read source info and end the drag.
        // Returns false if nothing is being dragged (e.g. a stale OnDrop).
        public bool TryConsumeDrag(out int srcIndex, out bool srcIsHotbar)
        {
            if (!IsDragging)
            {
                srcIndex = -1;
                srcIsHotbar = false;
                return false;
            }
            srcIndex = DragSourceIndex;
            srcIsHotbar = DragSourceIsHotbar;
            EndDrag();
            return true;
        }

        public void EndDrag()
        {
            IsDragging = false;
            _dragIconImage.gameObject.SetActive(false);

            OnDragEnded?.Invoke();
        }
    }
}
