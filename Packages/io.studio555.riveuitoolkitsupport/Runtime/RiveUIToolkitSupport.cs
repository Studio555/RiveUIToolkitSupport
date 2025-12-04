using System.Collections.Generic;
using Rive.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace io.studio555.riveuitoolkitsupport {
    [DefaultExecutionOrder(-1000)]
    public class RiveUIToolkitSupport : MonoBehaviour {
        private static RiveUIToolkitSupport _instance;
        private static bool _isQuitting;

        public static RiveUIToolkitSupport Instance {
            get {
                if (_isQuitting) return null;
                if (_instance != null) return _instance;

                _instance = FindFirstObjectByType<RiveUIToolkitSupport>();
                if (_instance != null) return _instance;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    return null;
#endif
                var go = new GameObject(nameof(RiveUIToolkitSupport));
                _instance = go.AddComponent<RiveUIToolkitSupport>();
                return _instance;
            }
        }

        private IRenderTargetStrategy _renderTargetStrategy;
        private readonly Dictionary<RiveElement, GameObject> _registeredElements = new();

        private void Awake() {
            if (_instance != null && _instance != this) {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void OnApplicationQuit() {
            _isQuitting = true;
        }

        private void OnDestroy() {
            if (_instance == this)
                _instance = null;
        }

        private void Initialize() {
            var renderTargetStrategyGo = new GameObject("RenderTargetStrategy");
            renderTargetStrategyGo.transform.SetParent(_instance.transform);
            var pooledRenderTargetStrategy = renderTargetStrategyGo.AddComponent<PooledRenderTargetStrategy>();
           // pooledRenderTargetStrategy.Configure(new Vector2Int(2048, 2048), 1, 3, PooledRenderTargetStrategy.PoolOverflowBehavior.Flexible);
            
            _renderTargetStrategy = pooledRenderTargetStrategy;
            
        }

        public RiveWidget Register(RiveElement riveElement) {
            if (_isQuitting) {
                return null;
            }

            Debug.Log($"[RiveUIToolkitSupport] Register {riveElement} {riveElement.RiveAsset}");
            var riveElementGo = new GameObject("RiveElement - " + riveElement.InstanceId);
            riveElementGo.transform.SetParent(_instance.transform);

            var rivePanel = riveElementGo.AddComponent<RivePanel>();
            var simpleRenderTargetStrategy = rivePanel.gameObject.GetComponent<SimpleRenderTargetStrategy>();
            if (simpleRenderTargetStrategy != null) {
                Destroy(simpleRenderTargetStrategy);
            }

            rivePanel.RenderTargetStrategy = _renderTargetStrategy;

            var riveWidgetGo = new GameObject("RiveWidget");
            riveWidgetGo.transform.SetParent(riveElementGo.transform);

            var riveWidget = riveWidgetGo.AddComponent<RiveWidget>();
            riveWidget.Load(riveElement.RiveAsset);
            
            riveElement.style.backgroundImage =
                new StyleBackground(Background.FromRenderTexture(rivePanel.RenderTexture));
            _registeredElements[riveElement] = riveElementGo;
            return riveWidget;
        }

        public void Unregister(RiveElement riveElement) {
            if (_isQuitting) {
                return;
            }

            if (!_registeredElements.TryGetValue(riveElement, out var go)) {
                return;
            }

            Debug.Log($"[RiveUIToolkitSupport] Unregister {riveElement}");
            Destroy(go);
            _registeredElements.Remove(riveElement);
        }
    }
}