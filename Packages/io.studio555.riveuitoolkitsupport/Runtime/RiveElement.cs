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
        
        private Asset _riveAsset;
        
        private Fit _fit = Fit.Contain;
        
        public RiveWidget Widget => _widget;
        
        [UxmlAttribute]
        public Asset RiveAsset
        {
            get => _riveAsset;
            set
            {
                if (_widget != null)
                {
                    _riveAsset = value;
                    _widget.Load(_riveAsset);
                    _widget.Fit = _fit;
                }
                else
                {
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

        private bool _isRegistered;

        public readonly string InstanceId = System.Guid.NewGuid().ToString();

        public RiveElement() {
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanelEvent);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanelEvent);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChangedEvent);
        }

        private void OnGeometryChangedEvent(GeometryChangedEvent evt) {
            var isVisible = evt.newRect is { width: > 0, height: > 0 };
            if (_widget != null) {
                _widget.enabled = isVisible;
            }
            if (_rivePanel != null) {
                _rivePanel.enabled = isVisible;

                if (isVisible) {
                    UpdateBackgroundFromPanel();
                }
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
        

        private void RegisterOnce() {
            if (_isRegistered) {
                return;
            }

            var instance = RiveUIToolkitSupport.Instance;
            if (!instance) {
                return;
            }

            (_widget, _rivePanel) = instance.Register(this);
            _widget.Fit = _fit;
            _widget.HitTestBehavior = HitTestBehavior.None;

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

            // Clear background so we don't hold onto pooled RenderTextures
            style.backgroundImage = default;

            _widget = null;
            _rivePanel = null;
            instance.Unregister(this);
            _isRegistered = false;
        }

        private void UpdateBackgroundFromPanel() {
            if (_rivePanel == null) {
                return;
            }

            var rt = _rivePanel.RenderTexture;
            if (rt == null) {
                return;
            }

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