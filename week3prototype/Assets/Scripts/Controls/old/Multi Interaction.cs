using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class MultiInteraction : MonoBehaviour
{
    //reference HealthController script 
    public HealthController healthControl; 

    public InputActionReference actionRef; 
    // single tap: slows down the scrolling every time you tap, up to a certain point, if its already slowed down below the point, it will speed up the scrolling up to a certain point
    // hold: starts the scrolling once you release, and the longer you held, the faster it scrolls - if it is already scrolling, it will stop the scrolling
    // multi tap: slows the scrolling, and initiates a "like" action
    [Header("Tap Mechanic Settings")]
    public ScrollMechanic scrollMechanic; // Reference to the ScrollMechanic script
    public float SingleTapSlowAmount = 0.6f; // Slows down tap by this factor
    public float MultiTapSlowAmount = 0.8f; // Amount to slow down the scrolling on multi tap
    public float tapSpeedThreshold = 1.5f; // if below this, tap will speed up instead
    public float tapSpeedUpAmount = 0.5f; // Amount to speed up the scrolling on tap if below threshold
    [Header("Hold Mechanic Settings")]
    public float maxHoldDuration = 3f; // max duration for speedup calc
    public float holdSpeedMultiplier = 10f; // Multiplier for scrolling speed up on hold release

    [Header("Scroll Speed Limits")]
    public float minSpeed = -30f; // Minimum speed for scrolling
    public float maxSpeed = 30f; // Maximum speed for scrolling

    [Header("Audio Settings")]
    public AudioSource audioSource;
    // public AudioClip tapClickSound; // A "thud" or "select" sound
    public AudioClip speedUpSound;   // Assign your bright/fast sound here
    public AudioClip slowDownSound; 
    [Header("Audio Modulation")]
    [Range(1f, 2f)] public float speedUpPitch = 1.2f;
    [Range(0f, 1f)] public float slowDownPitch = 0.7f;
    [Range(0f, 1f)] public float speedUpVolume = 1.0f;
    [Range(0f, 1f)] public float slowDownVolume = 0.6f;
    
    private float holdStartTime; // Time when hold interaction started

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable() {
        actionRef.action.Enable();
    }
    void OnDisable() {
        actionRef.action.Disable();

    }

    void processSingleTap() {
        if (scrollMechanic == null) {
            Debug.LogError("ScrollMechanic reference is not assigned in the inspector.");
            return;
        }
        // 1. Get current velocity and its direction
        float velocity = scrollMechanic.Inertia;
        float direction = Mathf.Sign(velocity); // Returns 1 if positive, -1 if negative
        float currentSpeed = Mathf.Abs(velocity); // Absolute speed (always positive)

        // Handle case where scroll is stopped (Sign(0) is 1, which is fine, defaults to up)
        if (velocity == 0) direction = 1; // Optional: Choose default direction if stopped

        if (currentSpeed < tapSpeedThreshold)
        {
            // SPEED UP: Add speed to the magnitude, relying on direction later
            Debug.Log("Speeding up from " + currentSpeed);
            currentSpeed += tapSpeedUpAmount;
            if (audioSource != null) {
                audioSource.pitch = speedUpPitch; // e.g. 1.2
                audioSource.PlayOneShot(speedUpSound, speedUpVolume);
            }
        } 
        else 
        {
            // SLOW DOWN: Multiply the magnitude
            Debug.Log("Slowing down from " + currentSpeed);
            currentSpeed *= SingleTapSlowAmount; 
            if (audioSource != null) {
                audioSource.pitch = slowDownPitch; // e.g. 0.7
                audioSource.PlayOneShot(slowDownSound, slowDownVolume);
            }
        }

        // 2. Clamp the MAGNITUDE (Speed), not the raw Inertia
        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);

        // 3. Re-apply the direction to the scroll mechanic
        scrollMechanic.Inertia = currentSpeed * direction;
        // --- ADD AUDIO ---
        // if (audioSource != null && tapClickSound != null) {
        //     // Reset pitch to normal for taps (in case the scroll script changed it)
        //     audioSource.pitch = 1f; 
        //     audioSource.PlayOneShot(tapClickSound);
        // }
        // -----------------
    }
    void Start()
    {
        healthControl = GameObject.FindGameObjectWithTag("HealthTag").GetComponent<HealthController>(); 

        if (actionRef == null) {
            Debug.LogError("InputActionReference is not assigned in the inspector.");
            return;
        }
        if (!(actionRef.action.interactions.Contains("Hold") 
        && actionRef.action.interactions.Contains("Tap") 
        && actionRef.action.interactions.Contains("MultiTap"))) {
            Debug.LogError("InputAction does not contain the required interactions: Hold, Tap, MultiTap.");
            return;
            
        }

        actionRef.action.started += ctx => {
            if (ctx.interaction is HoldInteraction) {
                // Debug.Log("Hold interaction started");
            } else if (ctx.interaction is TapInteraction) {
                // Debug.Log("Tap interaction started");
            } else if (ctx.interaction is MultiTapInteraction) {
                // Debug.Log("MultiTap interaction started");
            }
        };

        actionRef.action.performed += ctx => {
            if (ctx.interaction is HoldInteraction) {
                Debug.Log("Hold interaction performed");
            } else if (ctx.interaction is TapInteraction) {
                Debug.Log("Tap interaction performed");
                processSingleTap();
            } else if (ctx.interaction is MultiTapInteraction) {
                Debug.Log("MultiTap interaction performed");
            }
        };

        actionRef.action.canceled += ctx => {
            if (ctx.interaction is HoldInteraction) {
                // Debug.Log("Hold interaction canceled");
            } else if (ctx.interaction is TapInteraction) {
                // Debug.Log("Tap interaction canceled");
            } else if (ctx.interaction is MultiTapInteraction) {
                // Debug.Log("MultiTap interaction canceled");
            }
        };

    }

    // Update is called once per frame
    void Update()
    {
        //actionRef.action.GetTimeCompletedPercentage();
        
        //float currVelocity = scrollMechanic.Inertia;
        //float currSpeed = Mathf.Abs(currVelocity);
        //float proportion = currSpeed / 20;
        //Debug.Log("fill amount: " + proportion);


        //if(healthControl == null)
        //{
        //    Debug.Log("could not find health control");
        //}
        //healthControl.UpdateHealthBar(gameObject, 10, 20); 

        

    }
    
    
}
