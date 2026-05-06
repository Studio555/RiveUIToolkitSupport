using System.Collections.Generic;
using io.studio555.riveuitoolkitsupport;
using Rive;
using UnityEngine;
using UnityEngine.UIElements;

public class Sample : MonoBehaviour {
    [SerializeField] private UIDocument uiDocument;

    [Tooltip("Drag .riv Asset sub-objects here. The grid cycles through them.")]
    [SerializeField] private Asset[] riveAssets;

    [Tooltip("Number of RiveElements to spawn in the stress grid.")]
    [SerializeField] private int stressCount = 60;

    private static readonly int[] InitialSizes = { 48, 64, 96, 128 };
    private static readonly int[] ResizeSizes = { 32, 48, 64, 96, 128, 160 };

    private readonly List<RiveElement> _gridElements = new();
    private RiveElement _roboDude;
    private VisualElement _gridContent;
    private Label _statusLabel;
    private int _assetRotation;
    private System.Random _rng;

    public void Start() {
        if (uiDocument == null) {
            Debug.LogError("[Sample] UIDocument missing.");
            return;
        }
        var root = uiDocument.rootVisualElement;
        if (root == null) {
            Debug.LogError("[Sample] UIDocument.rootVisualElement is null.");
            return;
        }

        _rng = new System.Random(0xC0FFEE);
        _roboDude = root.Q<RiveElement>("RoboDude");
        _statusLabel = root.Q<Label>("StatusLabel");
        _gridContent = root.Q<VisualElement>("GridContent");

        root.Q<Button>("ToggleSampleButton").clicked += ToggleRoboDude;
        root.Q<Button>("ToggleHalfButton").clicked += ToggleEveryOther;
        root.Q<Button>("SwapAssetsButton").clicked += RotateAssets;
        root.Q<Button>("ResizeButton").clicked += RandomizeSizes;
        root.Q<Button>("RebuildButton").clicked += RebuildGrid;

        if (riveAssets == null || riveAssets.Length == 0) {
            SetStatus("No Rive assets assigned on the Sample component — drag .riv sub-objects into 'Rive Assets'.");
            return;
        }

        BuildGrid();
    }

    private void BuildGrid() {
        ClearGrid();
        for (int i = 0; i < stressCount; i++) {
            var size = InitialSizes[i % InitialSizes.Length] + _rng.Next(-8, 9);
            var element = new RiveElement {
                name = "Grid_" + i,
                RiveAsset = riveAssets[i % riveAssets.Length],
            };
            element.style.width = size;
            element.style.height = size;
            element.style.marginLeft = 4;
            element.style.marginRight = 4;
            element.style.marginTop = 4;
            element.style.marginBottom = 4;
            element.style.backgroundColor = new StyleColor(new UnityEngine.Color(1f, 1f, 1f, 0.06f));
            _gridContent.Add(element);
            _gridElements.Add(element);
        }
        UpdateStatus();
    }

    private void ClearGrid() {
        foreach (var e in _gridElements) {
            e.RemoveFromHierarchy();
        }
        _gridElements.Clear();
    }

    private void RebuildGrid() {
        BuildGrid();
    }

    private void ToggleRoboDude() {
        if (_roboDude == null) return;
        _roboDude.style.display = _roboDude.style.display == DisplayStyle.None
            ? DisplayStyle.Flex
            : DisplayStyle.None;
    }

    private void ToggleEveryOther() {
        for (int i = 0; i < _gridElements.Count; i += 2) {
            var e = _gridElements[i];
            e.style.display = e.style.display == DisplayStyle.None
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }
        UpdateStatus();
    }

    private void RotateAssets() {
        if (riveAssets == null || riveAssets.Length == 0) return;
        _assetRotation = (_assetRotation + 1) % riveAssets.Length;
        for (int i = 0; i < _gridElements.Count; i++) {
            _gridElements[i].RiveAsset = riveAssets[(i + _assetRotation) % riveAssets.Length];
        }
        UpdateStatus();
    }

    private void RandomizeSizes() {
        for (int i = 0; i < _gridElements.Count; i++) {
            var size = ResizeSizes[_rng.Next(ResizeSizes.Length)];
            _gridElements[i].style.width = size;
            _gridElements[i].style.height = size;
        }
        UpdateStatus();
    }

    private void UpdateStatus() {
        if (_statusLabel == null) return;
        int visible = 0;
        foreach (var e in _gridElements) {
            if (e.style.display != DisplayStyle.None) visible++;
        }
        _statusLabel.text = $"{visible}/{_gridElements.Count} visible | rotation={_assetRotation}";
    }

    private void SetStatus(string text) {
        if (_statusLabel != null) _statusLabel.text = text;
    }
}
