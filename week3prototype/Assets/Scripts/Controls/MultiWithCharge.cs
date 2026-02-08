using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class MultiWithCharge : MonoBehaviour
{
    public InputActionReference actionRef; 

    [Header("Dependencies")]
    public ScrollMechanic scrollMechanic; 

    [Header("Tap Settings")]
    public float SingleTapSlowAmount = 0.6f; 
    public float MultiTapSlowAmount = 0.9f; 
    public float tapSpeedThreshold = 5f; 
    public float tapSpeedUpAmount = 0.4f; 

    [Header("Speed Clamps")]
    public float minSpeed = -300f; // Minimum speed for scrolling
    public float maxSpeed = 300f; // Maximum speed for scrolling

    [Header("Hold Mechanic Settings")]
    public float maxHoldDuration = 2.0f; // Time to reach max charge
    public float minHoldPower = 10f;     // Minimum launch velocity
    public float maxHoldPower = 80f;     // Maximum launch velocity (at max duration)
    public float finalLoopDuration = 0.5f; // How many seconds from the end to loop
    
    [Header("Visual Juice (Shake & Zoom)")]
    public float shakeIntensity = 15f;    // Increased default for UI (Pixels are smaller than World Units)
    public float zoomAmount = 1.1f;       // Scale Multiplier (1.1 = 110% size)
    
    // Internal storage for UI positions
    private Vector2 originalCanvasPos;    
    private Vector3 originalCanvasScale;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioSource loopAudioSource; // *NEW* Separate source for looping charge sound
    
    [Header("Audio Clips")]
    public AudioClip speedUpSound;   
    public AudioClip slowDownSound; 
    public AudioClip chargeBuildupSound;   // rising sound
    public AudioClip maxChargeSound;   //  "Ready" sound
    public AudioClip launchSound;      // "Whoosh" sound
    public AudioClip chargeLoopSound;

    [Header("Audio Modulation")]
    [Range(1f, 3f)] public float maxChargePitch = 2.0f; // Pitch at max charge
    [Range(1f, 2f)] public float speedUpPitch = 1.55f;
    [Range(0f, 1f)] public float slowDownPitch = 0.3f;
    [Range(0f, 1f)] public float speedUpVolume = 0.4f;
    [Range(0f, 1f)] public float slowDownVolume = 0.75f;
    
    // Internal State
    private bool isHolding = false;
    private float holdTimer = 0f;
    private bool hasPlayedMaxSound = false;
    private bool isLoopingCharge = false;
    // private float lastAudioTime = -1f;

    void OnEnable() {
        if(actionRef != null) actionRef.action.Enable();
    }
    void OnDisable() {
        if(actionRef != null) actionRef.action.Disable();
    }

    void Start()
    {
        if (actionRef == null || scrollMechanic == null) {
            Debug.LogError("References missing in MultiInteraction.");
            return;
        }
        if (!(actionRef.action.interactions.Contains("Hold") 
        && actionRef.action.interactions.Contains("Tap") 
        && actionRef.action.interactions.Contains("MultiTap"))) {
            Debug.LogError("InputAction does not contain the required interactions: Hold, Tap, MultiTap.");
            return;
            
        }
        if (audioSource == null) {
            Debug.LogWarning("AudioSource reference is missing. Audio feedback will not work.");
        }
        if (loopAudioSource == null) {
            Debug.LogWarning("Loop AudioSource reference is missing. Looping charge sound will not work.");
        }

        // Store original camera settings for reset later
        if(scrollMechanic.targetCanvas != null) {
            originalCanvasPos = scrollMechanic.targetCanvas.anchoredPosition;
            originalCanvasScale = scrollMechanic.targetCanvas.localScale;
        }

        // Setup Interaction Callbacks
        actionRef.action.started += ctx => {
            if (ctx.interaction is TapInteraction) {
                // We handle tap in 'performed'
            } 
            else if (ctx.interaction is HoldInteraction) {
                
            }
        };

        actionRef.action.performed += ctx => {
            if (ctx.interaction is TapInteraction) {
                processSingleTap();
            } 
            else if(ctx.interaction is HoldInteraction)
            {
                // We use 'performed' to catch the hold without overlapping with tap's 'started'
                StartHold(); 
                Debug.Log("[Charge] Hold PERFORMED");
            }
            else if(ctx.interaction is MultiTapInteraction)
            {
                Debug.Log("[Charge] MultiTap PERFORMED - Slow down!");
            }
        };

        actionRef.action.canceled += ctx => {
            // If we were holding, this release triggers the launch
            if (isHolding) {
                ReleaseHold();
            }
        };
    }

    void StartHold()
    {
         Debug.Log("[Charge] StartHold called");
        // Slow existing motion before charging
        if (Mathf.Abs(scrollMechanic.Inertia) > 1.0f)
            scrollMechanic.Inertia *= 0.5f;

        isHolding = true;
        holdTimer = 0f;
        hasPlayedMaxSound = false;
        isLoopingCharge = false;

        // --- AUDIO: Play buildup ---
        if (loopAudioSource != null && chargeBuildupSound != null)
        {
            loopAudioSource.Stop();
            loopAudioSource.clip = chargeBuildupSound;
            Debug.Log("[Charge] Playing BUILDUP clip");
            loopAudioSource.loop = false;
            loopAudioSource.pitch = 1f;
            loopAudioSource.Play();
        }
    }

    void ReleaseHold()
    {
        if (!isHolding) return;

        isHolding = false;
        isLoopingCharge = false;

        // 1. Stop Charge Audio/Effects
        if (loopAudioSource != null) loopAudioSource.Stop();
        ResetVisuals();

        // 2. Calculate Launch Power
        // Ratio 0 to 1 based on how long we held
        float ratio = Mathf.Clamp01(holdTimer / maxHoldDuration); 
        
        // If held for a tiny fraction, ignore (prevents accidental tiny launches)
        if (ratio < 0.1f) return; 

        float launchVelocity = Mathf.Lerp(minHoldPower, maxHoldPower, ratio);

        // 3. Apply Velocity (Launch Upwards by default, or flip based on need)
        scrollMechanic.Inertia = launchVelocity; // Positive = Scroll Up, Negative = Down

        // 4. Play Launch Sound
        if (audioSource != null && launchSound != null) {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(launchSound);
        }
    }

    // Update controls the continuous "Juice" (Shake, Zoom, Audio Pitch)
    void Update()
    {   
        // Robustness: If Input System says button is NOT pressed, force release
        // This handles cases where 'canceled' might not fire in edge cases
        // if (isHolding && !actionRef.action.IsPressed()) {
        //     ReleaseHold();
        // }

        if (isHolding)
        {
            holdTimer += Time.deltaTime;
            float ratio = Mathf.Clamp01(holdTimer / maxHoldDuration);

            // --- 1. VISUALS: Zoom & Shake ---
            if (scrollMechanic.targetCanvas != null)
            {
                // Zoom: We Scale the UI Up (e.g. 1.0 -> 1.1)
                // Note: Ensure your TargetCanvas pivot is in the center (0.5, 0.5) for best results
                Vector3 targetScale = originalCanvasScale * zoomAmount;
                scrollMechanic.targetCanvas.localScale = Vector3.Lerp(originalCanvasScale, targetScale, ratio);

                // Shake: We move the AnchoredPosition
                float currentShake = shakeIntensity * ratio; 
                Vector2 shakeOffset = UnityEngine.Random.insideUnitCircle * currentShake;
                scrollMechanic.targetCanvas.anchoredPosition = originalCanvasPos + shakeOffset;
            }

            // --- AUDIO: Pitch ramp + loop switch ---
            if (loopAudioSource != null)
            {
                 // Log state changes once
                // if (loopAudioSource.time != lastAudioTime)
                // {
                //     Debug.Log(
                //         $"[Charge][Audio] clip={loopAudioSource.clip?.name} " +
                //         $"time={loopAudioSource.time:F2} / {loopAudioSource.clip?.length:F2} " +
                //         $"isPlaying={loopAudioSource.isPlaying} " +
                //         $"looping={isLoopingCharge}"
                //     );
                //     lastAudioTime = loopAudioSource.time;
                // }
            
                if(loopAudioSource.isPlaying)
                {
                    // Pitch ramps smoothly regardless of clip
                    loopAudioSource.pitch = Mathf.Lerp(1f, maxChargePitch, ratio);
                }

                    // When buildup finishes, switch to looping clip
                    if (!isLoopingCharge &&
                        loopAudioSource.clip == chargeBuildupSound &&
                        !loopAudioSource.isPlaying)
                    {
                        Debug.Log("[Charge] Buildup finished → switching to LOOP");
                        isLoopingCharge = true;

                        loopAudioSource.Stop();
                        loopAudioSource.clip = chargeLoopSound;
                        loopAudioSource.loop = true;
                        loopAudioSource.pitch = Mathf.Lerp(1f, maxChargePitch, ratio);
                        loopAudioSource.Play();
                    }
                

                // --- 3. MAX CHARGE EVENT ---
                if (ratio >= 1.0f && !hasPlayedMaxSound)
                {
                    hasPlayedMaxSound = true;
                    if (audioSource != null && maxChargeSound != null) {
                        audioSource.PlayOneShot(maxChargeSound);
                    }

                }
             }
        }
    }

    void ResetVisuals()
    {
        // Restore original UI state
        if (scrollMechanic.targetCanvas != null) {
            scrollMechanic.targetCanvas.anchoredPosition = originalCanvasPos;
            scrollMechanic.targetCanvas.localScale = originalCanvasScale;
        }
    }

    void processSingleTap() {
        if (scrollMechanic == null) return;

        // If we just released a hold, don't process a tap immediately
        if (Time.time - holdTimer < 0.1f && isHolding) return; 

        float velocity = scrollMechanic.Inertia;
        float direction = Mathf.Sign(velocity);
        float currentSpeed = Mathf.Abs(velocity);
        if (velocity == 0) direction = 1;

        if (currentSpeed < tapSpeedThreshold) {
            Debug.Log($"[Tap] Speed up! Current: {currentSpeed:F2} + {tapSpeedUpAmount}");
            currentSpeed += tapSpeedUpAmount;

            if (audioSource != null) {
                audioSource.pitch = speedUpPitch;
                audioSource.PlayOneShot(speedUpSound, speedUpVolume);
            }
        } 
        else {
            Debug.Log($"[Tap] Slow down! Current: {currentSpeed:F2} * {SingleTapSlowAmount}");
            currentSpeed *= SingleTapSlowAmount; 
            if (audioSource != null) {
                audioSource.pitch = slowDownPitch;
                audioSource.PlayOneShot(slowDownSound, slowDownVolume);
            }
        }

        // Clamp & Apply
        scrollMechanic.Inertia = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed) * direction; // Increased clamp for launch fun
    }
}
