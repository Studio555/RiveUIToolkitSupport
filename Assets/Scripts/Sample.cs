using io.studio555.riveuitoolkitsupport;
using UnityEngine;
using UnityEngine.UIElements;

public class Sample : MonoBehaviour {
    [SerializeField] private UIDocument uiDocument;
    
    private RiveElement _roboDude;
    
    public void Start() {
        var root = uiDocument.rootVisualElement;
        _roboDude = root.Q<RiveElement>("RoboDude");
        Debug.Log("RiveElement found: " + (_roboDude != null));

        root.Q<Button>("ToggleButton").clicked += () => {
            _roboDude.style.display = _roboDude.style.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None;
        };

    }
    
    public void Update() {
        
    }
    
}