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

        // Cap on how many parked panel GameObjects we keep alive between registrations.
        // Each parked entry retains its RivePanel + SimpleRenderTargetStrategy + RenderTexture,
        // so reusing them avoids the WaitForEndOfFrame renderer-release storm on scene transitions.
        private const int MaxParkedElements = 32;

        private readonly Dictionary<RiveElement, PanelEntry> _activeEntries = new();
        private readonly Stack<PanelEntry> _parkedEntries = new();

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

            PanelEntry entry;
            if (_parkedEntries.Count > 0) {
                entry = _parkedEntries.Pop();
                ReuseParkedEntry(entry, riveElement, pixelW, pixelH);
            } else {
                entry = CreateNewEntry(riveElement, pixelW, pixelH);
            }

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

            if (entry.Root == null) {
                return;
            }

            if (_parkedEntries.Count < MaxParkedElements) {
                entry.Root.SetActive(false);
                entry.Root.name = "RiveElement (parked)";
                _parkedEntries.Push(entry);
            } else {
                Destroy(entry.Root);
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

        private static void ReuseParkedEntry(PanelEntry entry, RiveElement riveElement, int pixelW, int pixelH) {
            entry.Root.name = "RiveElement - " + riveElement.InstanceId;
            // Resize before activation so the panel's first redraw on OnEnable already
            // sees the right size; avoids an extra RT-resize round trip.
            var size = entry.Rect.sizeDelta;
            if ((int)size.x != pixelW || (int)size.y != pixelH) {
                entry.Rect.sizeDelta = new Vector2(pixelW, pixelH);
            }
            entry.Widget.Load(riveElement.RiveAsset);
            entry.Root.SetActive(true);
        }
    }
}
