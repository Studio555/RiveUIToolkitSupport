using System.Collections.Generic;
using Rive.Components;
using UnityEngine;

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

        private readonly Dictionary<RiveElement, GameObject> _registeredElements = new();

        public readonly struct Registration {
            public readonly RiveWidget Widget;
            public readonly RivePanel Panel;
            public readonly RectTransform PanelRect;

            public Registration(RiveWidget widget, RivePanel panel, RectTransform panelRect) {
                Widget = widget;
                Panel = panel;
                PanelRect = panelRect;
            }
        }

        private void Awake() {
            if (_instance != null && _instance != this) {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnApplicationQuit() {
            _isQuitting = true;
        }

        private void OnDestroy() {
            if (_instance == this)
                _instance = null;
        }

        public Registration Register(RiveElement riveElement, Vector2Int initialPixelSize) {
            if (_isQuitting) {
                return default;
            }

            // Create with RectTransform up front so it's already sized when RivePanel.OnEnable
            // registers with its auto-attached SimpleRenderTargetStrategy and queues the first draw.
            var riveElementGo = new GameObject(
                "RiveElement - " + riveElement.InstanceId,
                typeof(RectTransform));
            var rect = (RectTransform)riveElementGo.transform;
            rect.SetParent(_instance.transform, worldPositionStays: false);
            rect.sizeDelta = new Vector2(
                Mathf.Max(1, initialPixelSize.x),
                Mathf.Max(1, initialPixelSize.y));

            var rivePanel = riveElementGo.AddComponent<RivePanel>();

            var riveWidgetGo = new GameObject("RiveWidget", typeof(RectTransform));
            var widgetRect = (RectTransform)riveWidgetGo.transform;
            widgetRect.SetParent(rect, worldPositionStays: false);
            // Stretch the widget to fill the panel so the artboard's Fit calculation
            // uses the full panel rect rather than the default 100x100.
            widgetRect.anchorMin = Vector2.zero;
            widgetRect.anchorMax = Vector2.one;
            widgetRect.pivot = new Vector2(0.5f, 0.5f);
            widgetRect.sizeDelta = Vector2.zero;
            widgetRect.anchoredPosition = Vector2.zero;

            var riveWidget = riveWidgetGo.AddComponent<RiveWidget>();
            riveWidget.Load(riveElement.RiveAsset);

            _registeredElements[riveElement] = riveElementGo;
            return new Registration(riveWidget, rivePanel, rect);
        }

        public void Unregister(RiveElement riveElement) {
            if (_isQuitting) {
                return;
            }

            if (!_registeredElements.TryGetValue(riveElement, out var go)) {
                return;
            }

            Destroy(go);
            _registeredElements.Remove(riveElement);
        }
    }
}
