using Rive;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using io.studio555.riveuitoolkitsupport;

public class AssetSwitcher : MonoBehaviour
{
    public List<Rive.Asset> assetList;
    public UIDocument uiDocument;
    private RiveElement switchableRiveElement;
    private int currentAssetIndex = 0;

    void Start()
    {
        if (uiDocument != null)
        {
            VisualElement root = uiDocument.rootVisualElement;
            switchableRiveElement = root.Q<RiveElement>("SwitchableRiveElement");
            if (switchableRiveElement != null && assetList != null && assetList.Count > 0)
            {
                switchableRiveElement.RiveAsset = assetList[currentAssetIndex];
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (switchableRiveElement != null && assetList != null && assetList.Count > 0)
            {
                currentAssetIndex = (currentAssetIndex + 1) % assetList.Count;
                switchableRiveElement.RiveAsset = assetList[currentAssetIndex];
                Debug.Log("SwitchableRiveElement: " + switchableRiveElement.RiveAsset);
            }
        }
    }
}
