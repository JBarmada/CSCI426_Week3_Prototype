using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class MultiInteraction : MonoBehaviour
{

    public InputActionReference actionRef; 
    // single tap: slows down the scrolling every time you tap, up to a certain point, if its already slowed down below the point, it will speed up the scrolling up to a certain point
    // hold: starts the scrolling once you release, and the longer you held, the faster it scrolls - if it is already scrolling, it will stop the scrolling
    // multi tap: slows the scrolling, and initiates a "like" action
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

        actionRef.action.canceled += ctx => {
            if (ctx.interaction is HoldInteraction) {
                Debug.Log("Hold interaction canceled");
            } else if (ctx.interaction is TapInteraction) {
                Debug.Log("Tap interaction canceled");
            } else if (ctx.interaction is MultiTapInteraction) {
                Debug.Log("MultiTap interaction canceled");
            }
        };

    }

    // Update is called once per frame
    void Update()
    {   
        //actionRef.action.GetTimeCompletedPercentage();
    }
    
    
}
