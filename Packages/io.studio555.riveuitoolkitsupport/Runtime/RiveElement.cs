using Rive;
using Rive.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace io.studio555.riveuitoolkitsupport {
    [UxmlElement]
    public partial class RiveElement : VisualElement
    {
        private RiveWidget _widget;
        private Asset _riveAsset;
        [UxmlAttribute]
        public Asset RiveAsset
        {
            get => _riveAsset;
            set
            {
                if (false) // (_widget != null)
                {
                    _riveAsset = value;
                    _widget.Load(_riveAsset);
                }
                else
                {
                    Unregister();
                    _riveAsset = value;
                    RegisterOnce();
                }
            }
        }

        private bool _isRegistered;

        public readonly string InstanceId = System.Guid.NewGuid().ToString();

        public RiveElement() {
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanelEvent);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanelEvent);
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

            _widget = instance.Register(this);
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

            _widget = null;
            instance.Unregister(this);
            _isRegistered = false;
        }
    }
}