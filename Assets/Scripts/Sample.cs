using io.studio555.riveuitoolkitsupport;
using UnityEngine;
using UnityEngine.UIElements;

public class Sample : MonoBehaviour {
    [SerializeField] private UIDocument uiDocument;

    private RiveElement _recycleArea;
    
    public void Start() {
        var root = uiDocument.rootVisualElement;
        
        _recycleArea = root.Q<RiveElement>("RecycleArea");
        
        Debug.Log("RiveElement found: " + (_recycleArea != null));
        
    }
    
    public void Update() {
        if (_recycleArea != null) {
            
            
            // Example: Change the animation state based on some condition
            if (Input.GetKeyDown(KeyCode.Space)) {
                Debug.Log("Space key pressed - triggering animation");
                
            }
        }
    }
    
}