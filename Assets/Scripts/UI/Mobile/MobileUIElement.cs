using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using LushWorld.Save;

namespace LushWorld.UI.Mobile
{
    // Attach to every mobile UI RectTransform that should be customizable.
    // Sets an elementId in Inspector, registers with MobileLayoutCustomizer on Awake,
    // and becomes draggable during HUD edit mode.
    public class MobileUIElement : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [SerializeField] private string _elementId;

        private RectTransform _rt;
        private RectTransform _parentRT;
        private CanvasGroup   _group;
        private bool          _ownedGroup;   // true = we created the CanvasGroup

        private Vector2 _defaultPos;
        private Vector3 _defaultScale;
        private float   _defaultAlpha = 1f;

        private float   _savedAlpha;
        private bool    _savedInteractable;
        private bool    _savedBlocksRaycasts;
        private Vector2 _dragOffset;
        private bool    _editMode;
        private bool    _dragging;

        private Outline _outline;
        private Image   _ghostGraphic;   // added if root has no Graphic, so Outline renders
        private readonly List<(MonoBehaviour comp, bool wasEnabled)> _disabledHandlers = new();

        public string ElementId    => _elementId;
        public float  CurrentScale => _rt.localScale.x;
        public float  CurrentAlpha => _ownedGroup ? _group.alpha : 1f;
        public bool   SupportsAlpha => _ownedGroup;

        // Fired when the element is tapped in edit mode; customizer subscribes.
        public static event Action<MobileUIElement> OnSelected;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        void Awake()
        {
            _rt       = GetComponent<RectTransform>();
            _parentRT = transform.parent?.GetComponent<RectTransform>();

            var existing = GetComponent<CanvasGroup>();
            if (existing != null)
            {
                _group      = existing;
                _ownedGroup = false;
            }
            else
            {
                _group      = gameObject.AddComponent<CanvasGroup>();
                _ownedGroup = true;
            }

            _defaultPos   = _rt.anchoredPosition;
            _defaultScale = _rt.localScale;
            _defaultAlpha = _group.alpha;

            MobileLayoutCustomizer.Register(this);
        }

        void OnDestroy() => MobileLayoutCustomizer.Unregister(this);

        // ── Edit mode ──────────────────────────────────────────────────────────

        public void EnterEditMode()
        {
            _editMode               = true;
            _savedAlpha             = _group.alpha;
            _savedInteractable      = _group.interactable;
            _savedBlocksRaycasts    = _group.blocksRaycasts;

            // Force element visible so it can be found and positioned
            if (_group.alpha < 0.3f) _group.alpha = 0.8f;

            // Prevent child buttons/controls from firing while we're dragging
            _group.interactable   = false;
            _group.blocksRaycasts = true;

            // Outline requires a Graphic on the same GO; add a ghost if none exists
            if (GetComponent<Graphic>() == null)
            {
                _ghostGraphic              = gameObject.AddComponent<Image>();
                _ghostGraphic.color        = Color.clear;
                _ghostGraphic.raycastTarget = false;
            }

            _outline              ??= gameObject.AddComponent<Outline>();
            _outline.effectColor    = new Color(0.4f, 0.7f, 1f, 0.85f);
            _outline.effectDistance = new Vector2(3, -3);

            // CanvasGroup.interactable=false blocks Selectable (Button) but NOT raw
            // IPointerDownHandler components (e.g. UIVirtualButton). Disable them explicitly
            // so the underlying game action never fires during HUD customization.
            foreach (var comp in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (comp is MobileUIElement) continue;
                if (comp is IPointerDownHandler or IPointerUpHandler or Selectable)
                {
                    _disabledHandlers.Add((comp, comp.enabled));
                    comp.enabled = false;
                }
            }
        }

        public void ExitEditMode()
        {
            _editMode             = false;
            _group.interactable   = _savedInteractable;
            _group.blocksRaycasts = _savedBlocksRaycasts;

            foreach (var (comp, wasEnabled) in _disabledHandlers)
                if (comp != null) comp.enabled = wasEnabled;
            _disabledHandlers.Clear();

            // Restore alpha only if we don't own the group
            // (owned-group alpha was already set by ApplyLayout on load)
            if (!_ownedGroup) _group.alpha = _savedAlpha;

            if (_outline      != null) { Destroy(_outline);      _outline      = null; }
            if (_ghostGraphic != null) { Destroy(_ghostGraphic); _ghostGraphic = null; }
        }

        public void SetSelected(bool selected)
        {
            if (_outline == null) return;
            _outline.effectColor = selected
                ? new Color(1f, 0.85f, 0.2f, 1f)    // gold = selected
                : new Color(0.4f, 0.7f, 1f, 0.85f); // blue = unselected
        }

        // ── Layout API ────────────────────────────────────────────────────────

        public void SetScale(float scale) => _rt.localScale = Vector3.one * scale;

        public void SetAlpha(float alpha)
        {
            if (_ownedGroup) _group.alpha = alpha;
        }

        public void ResetToDefault()
        {
            _rt.anchoredPosition = _defaultPos;
            _rt.localScale       = _defaultScale;
            if (_ownedGroup) _group.alpha = _defaultAlpha;
        }

        public void ApplyLayout(MobileElementLayoutData d)
        {
            _rt.anchoredPosition = d.anchoredPosition;
            _rt.localScale       = Vector3.one * d.scale;
            if (_ownedGroup) _group.alpha = d.alpha;
        }

        public MobileElementLayoutData GetCurrentLayout() => new MobileElementLayoutData
        {
            elementId        = _elementId,
            anchoredPosition = _rt.anchoredPosition,
            scale            = _rt.localScale.x,
            alpha            = _ownedGroup ? _group.alpha : 1f,
        };

        // ── Drag handlers ─────────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData e)
        {
            if (!_editMode) return;
            _dragging = true;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parentRT, e.position, e.pressEventCamera, out var local);
            _dragOffset = _rt.anchoredPosition - local;
            _rt.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData e)
        {
            if (!_editMode || !_dragging) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parentRT, e.position, e.pressEventCamera, out var local);
            _rt.anchoredPosition = local + _dragOffset;
        }

        public void OnEndDrag(PointerEventData e) => _dragging = false;

        // ── Click → select ────────────────────────────────────────────────────

        public void OnPointerClick(PointerEventData e)
        {
            if (!_editMode || _dragging) return;
            OnSelected?.Invoke(this);
        }
    }
}
