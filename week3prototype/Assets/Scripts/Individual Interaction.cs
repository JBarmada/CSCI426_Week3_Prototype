using UnityEngine;
using UnityEngine.InputSystem;

public class IndividualInteraction : MonoBehaviour
{

    public InputActionReference singlePressAction; // Reference to the input action for single press
    public InputActionReference doublePressAction; // Reference to the input action for double press
    public InputActionReference holdAction; // Reference to the input action for hold

    private 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (singlePressAction == null || doublePressAction == null || holdAction == null) {
            Debug.LogError("One or more InputActionReferences are not assigned in the inspector.");
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {   
        
    }

    // fixedupdate: called at a fixed interval, used for physics updates
    void OnEnable() {
        singlePressAction.action.performed += single; 
        doublePressAction.action.performed += doublePress;
        holdAction.action.performed += hold;
    }
    void OnDisable() {
        singlePressAction.action.performed -= single; 
        doublePressAction.action.performed -= doublePress;
        holdAction.action.performed -= hold;
    }
    private void single(InputAction.CallbackContext context) {
        Debug.Log("Single Press Detected");
    }
    private void doublePress(InputAction.CallbackContext context) {
        Debug.Log("Double Press Detected");
    }
    private void hold(InputAction.CallbackContext context) {
        Debug.Log("Hold Detected");
    }
}
