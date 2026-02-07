using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class MultiInteraction : MonoBehaviour
{

    public InputActionReference actionRef; 

    private 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable() {
        actionRef.action.Enable();
    }
    void OnDisable() {
        actionRef.action.Disable();

    }
    void Start()
    {
        if (actionRef == null) {
            Debug.LogError("InputActionReference is not assigned in the inspector.");
            return;
        }
        if (!(actionRef.action.interactions.Contains("Hold") && actionRef.action.interactions.Contains("Tap") && actionRef.action.interactions.Contains("MultiTap"))) {
            Debug.LogError("InputAction does not contain the required interactions: Hold, Tap, MultiTap.");
            return;
            
        }

        actionRef.action.started += ctx => {
            if (ctx.interaction is HoldInteraction) {
                Debug.Log("Hold interaction started");
            } else if (ctx.interaction is TapInteraction) {
                Debug.Log("Tap interaction started");
            } else if (ctx.interaction is MultiTapInteraction) {
                Debug.Log("MultiTap interaction started");
            }
        };

        actionRef.action.performed += ctx => {
            if (ctx.interaction is HoldInteraction) {
                Debug.Log("Hold interaction performed");
            } else if (ctx.interaction is TapInteraction) {
                Debug.Log("Tap interaction performed");
            } else if (ctx.interaction is MultiTapInteraction) {
                Debug.Log("MultiTap interaction performed");
            }
        };

    }

    // Update is called once per frame
    void Update()
    {   
        
    }
    
    
}
