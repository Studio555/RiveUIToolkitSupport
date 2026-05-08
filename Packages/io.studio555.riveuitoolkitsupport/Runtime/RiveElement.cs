using Rive;
using Rive.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace io.studio555.riveuitoolkitsupport {
    [UxmlElement]
    public partial class RiveElement : VisualElement
    {
        private RiveWidget _widget;
        private RivePanel _rivePanel;
        private RectTransform _panelRect;

        private Asset _riveAsset;
        private Fit _fit = Fit.Contain;

        private RenderTexture _lastBoundRT;
        private bool _isRegistered;
        private bool? _lastVisible;

        private string _instanceId;

        public string InstanceId => _instanceId ??= System.Guid.NewGuid().ToString();
        public RiveWidget Widget => _widget;

        [UxmlAttribute]
        public Asset RiveAsset
        {
            get => _riveAsset;
            set
            {
                if (_widget != null) {
                    _riveAsset = value;
                    _widget.Load(_riveAsset);
                    _widget.Fit = _fit;
                } else {
                    Unregister();
                    _riveAsset = value;
                    RegisterOnce();
                }
            }
        }

        [UxmlAttribute]
        public Fit Fit
        {
            get => _fit;
            set
            {
                _fit = value;
                if (_widget != null) {
                    _widget.Fit = value;
                }
            }
        }

        public RiveElement() {
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanelEvent);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanelEvent);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChangedEvent);
        }

        private void OnGeometryChangedEvent(GeometryChangedEvent evt) {
            var rect = evt.newRect;
            var isVisible = rect is { width: > 0, height: > 0 };

            if (isVisible && _panelRect != null) {
                var pp = panel?.scaledPixelsPerPoint ?? 1f;
                var w = Mathf.Max(1, Mathf.RoundToInt(rect.width * pp));
                var h = Mathf.Max(1, Mathf.RoundToInt(rect.height * pp));
                var current = _panelRect.sizeDelta;
                if ((int)current.x != w || (int)current.y != h) {
                    _panelRect.sizeDelta = new Vector2(w, h);
                }
            }

            if (_lastVisible != isVisible) {
                _lastVisible = isVisible;
                if (_widget != null) {
                    _widget.enabled = isVisible;
                }
                if (_rivePanel != null) {
                    _rivePanel.enabled = isVisible;
                }
            }

            if (isVisible) {
                UpdateBackgroundFromPanel();
            }
        }

        private void OnAttachToPanelEvent(AttachToPanelEvent _) {
            if (!Application.isPlaying) {
                return;
            }
            RegisterOnce();
        }

        private void OnDetachFromPanelEvent(DetachFromPanelEvent _) {
            if (!Application.isPlaying) {
                return;
            }
            Unregister();
        }

        private Vector2Int CurrentPixelSize() {
            var rect = contentRect;
            var w = float.IsNaN(rect.width)  ? 0f : rect.width;
            var h = float.IsNaN(rect.height) ? 0f : rect.height;
            var pp = panel?.scaledPixelsPerPoint ?? 1f;
            return new Vector2Int(
                Mathf.Max(1, Mathf.RoundToInt(w * pp)),
                Mathf.Max(1, Mathf.RoundToInt(h * pp)));
        }

        private void RegisterOnce() {
            if (_isRegistered) {
                return;
            }

            var instance = RiveUIToolkitSupport.Instance;
            if (!instance) {
                return;
            }

            var registration = instance.Register(this, CurrentPixelSize());
            _widget = registration.Widget;
            _rivePanel = registration.Panel;
            _panelRect = registration.PanelRect;

            if (_widget != null) {
                _widget.Fit = _fit;
                _widget.HitTestBehavior = HitTestBehavior.None;
            }

            if (_rivePanel != null) {
                _rivePanel.OnRenderTargetUpdated += OnRenderTargetUpdated;
            }

            UpdateBackgroundFromPanel();
            _isRegistered = true;
        }

        private void Unregister() {
            if (!_isRegistered) {
                return;
            }

            var instance = RiveUIToolkitSupport.Instance;
            if (!instance) {
                return;
            }

            OnSupportReleased();
            instance.Unregister(this);
        }

        // Called by RiveUIToolkitSupport when it tears down this element's panel from its side
        // (per-element Unregister or bulk ReleaseAll). Clears element-side refs without calling
        // back into Support, so it's safe to invoke while iterating Support's active dict.
        internal void OnSupportReleased() {
            if (!_isRegistered) {
                return;
            }

            if (_rivePanel != null) {
                _rivePanel.OnRenderTargetUpdated -= OnRenderTargetUpdated;
            }

            // Drop the RT reference so we don't pin a soon-to-be-destroyed texture.
            style.backgroundImage = default;
            _lastBoundRT = null;
            _lastVisible = null;

            _widget = null;
            _rivePanel = null;
            _panelRect = null;

            _isRegistered = false;
        }

        private void OnRenderTargetUpdated() {
            UpdateBackgroundFromPanel();
        }

        private void UpdateBackgroundFromPanel() {
            if (_rivePanel == null) {
                return;
            }

            var rt = _rivePanel.RenderTexture;
            if (rt == null) {
                return;
            }

            if (ReferenceEquals(rt, _lastBoundRT)) {
                return;
            }

            _lastBoundRT = rt;
            style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rt));
        }

        public bool TryFireTrigger(string triggerName) {
            if (_widget == null) {
                Debug.LogWarning($"[RiveElement] Widget is null for {this}");
                return false;
            }

            var stateMachine = _widget.StateMachine;
            if (stateMachine == null) {
                Debug.LogWarning($"[RiveElement] StateMachine is null for {this}");
                return false;
            }
            var trigger = stateMachine.GetTrigger(name: triggerName);
            if (trigger == null) {
                Debug.LogWarning($"[RiveElement] Trigger '{triggerName}' not found in StateMachine for {this}");
                return false;
            }
            trigger.Fire();
            return true;
        }
    }
}
