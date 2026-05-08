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

        private readonly Dictionary<RiveElement, PanelEntry> _activeEntries = new();

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

        private class PanelEntry {
            public GameObject Root;
            public RectTransform Rect;
            public RivePanel Panel;
            public RiveWidget Widget;
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

            var pixelW = Mathf.Max(1, initialPixelSize.x);
            var pixelH = Mathf.Max(1, initialPixelSize.y);

            var entry = CreateNewEntry(riveElement, pixelW, pixelH);
            _activeEntries[riveElement] = entry;
            return new Registration(entry.Widget, entry.Panel, entry.Rect);
        }

        public void Unregister(RiveElement riveElement) {
            if (_isQuitting) {
                return;
            }

            if (!_activeEntries.TryGetValue(riveElement, out var entry)) {
                return;
            }
            _activeEntries.Remove(riveElement);

            if (entry.Root != null) {
                Destroy(entry.Root);
            }
        }

        // Tear down every active RiveElement registration immediately. Call before a scene
        // transition so RendererUtils.ReleaseRenderer's WaitForEndOfFrame coroutines fire now
        // (one batched stall) rather than competing with the next scene's load.
        public void ReleaseAll() {
            if (_isQuitting) return;
            if (_activeEntries.Count == 0) return;

            // Snapshot first: OnSupportReleased clears element-side state without calling back
            // into Unregister, but we still iterate a copy so the dict can be cleared up front.
            var count = _activeEntries.Count;
            var elements = new RiveElement[count];
            var entries = new PanelEntry[count];
            var i = 0;
            foreach (var kv in _activeEntries) {
                elements[i] = kv.Key;
                entries[i] = kv.Value;
                i++;
            }
            _activeEntries.Clear();

            for (var j = 0; j < count; j++) {
                elements[j].OnSupportReleased();
                if (entries[j].Root != null) {
                    Destroy(entries[j].Root);
                }
            }
        }

        private PanelEntry CreateNewEntry(RiveElement riveElement, int pixelW, int pixelH) {
            // Pre-size the RectTransform before adding RivePanel so the auto-attached
            // SimpleRenderTargetStrategy allocates its first RT at the correct size.
            var go = new GameObject(
                "RiveElement - " + riveElement.InstanceId,
                typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(_instance.transform, worldPositionStays: false);
            rect.sizeDelta = new Vector2(pixelW, pixelH);

            var rivePanel = go.AddComponent<RivePanel>();

            var widgetGo = new GameObject("RiveWidget", typeof(RectTransform));
            var widgetRect = (RectTransform)widgetGo.transform;
            widgetRect.SetParent(rect, worldPositionStays: false);
            // Stretch the widget to fill the panel so the artboard's Fit calculation
            // uses the full panel rect rather than the default 100x100.
            widgetRect.anchorMin = Vector2.zero;
            widgetRect.anchorMax = Vector2.one;
            widgetRect.pivot = new Vector2(0.5f, 0.5f);
            widgetRect.sizeDelta = Vector2.zero;
            widgetRect.anchoredPosition = Vector2.zero;

            var widget = widgetGo.AddComponent<RiveWidget>();
            widget.Load(riveElement.RiveAsset);

            return new PanelEntry {
                Root = go,
                Rect = rect,
                Panel = rivePanel,
                Widget = widget,
            };
        }
    }
}
