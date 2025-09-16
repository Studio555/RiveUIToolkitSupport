using Rive;
using UnityEngine.UIElements;

namespace io.studio555.riveuitoolkitsupport {
    [UxmlElement]
    public partial class RiveElement : VisualElement {
        [UxmlAttribute]
        public Asset RiveAsset;

        private bool _isRegistered;

        public readonly string InstanceId = System.Guid.NewGuid().ToString();
        
        public RiveElement() {
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanelEvent);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanelEvent);
        }
    
        private void OnAttachToPanelEvent(AttachToPanelEvent _) {
            RegisterOnce();
        }

        private void OnDetachFromPanelEvent(DetachFromPanelEvent _) {
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
        
            instance.Register(this);
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
            instance.Unregister(this);
            _isRegistered = false;
        
        }
    
    }
}