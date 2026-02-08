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
    public float shakeIntensity = 5f;    // How much to shake the UI/Camera
    public float zoomAmount = 0.8f;      // Target Ortho Size multiplier (e.g. 0.8 = 80% size)
    private float defaultOrthoSize;      // To store original camera size
    private Vector3 originalCamPos;      // To store original camera position

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioSource loopAudioSource; // *NEW* Separate source for looping charge sound
    
    [Header("Audio Clips")]
    public AudioClip speedUpSound;   
    public AudioClip slowDownSound; 
    public AudioClip chargingSound;    // *NEW* Loopable rising sound
    public AudioClip maxChargeSound;   // *NEW* "Ding" or "Ready" sound
    public AudioClip launchSound;      // *NEW* "Whoosh" sound

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

        // Store original camera settings for reset later
        if(scrollMechanic.camera != null) {
            defaultOrthoSize = scrollMechanic.camera.orthographicSize;
            originalCamPos = scrollMechanic.camera.transform.position;
        }

        // Setup Interaction Callbacks
        actionRef.action.started += ctx => {
            if (ctx.interaction is TapInteraction) {
                // Tap logic is immediate
            } 
            else if (ctx.interaction is HoldInteraction || ctx.interaction is SlowTapInteraction) {
                // We use 'started' to catch the very beginning of the press
                StartHold(); 
            }
        };

        actionRef.action.performed += ctx => {
            if (ctx.interaction is TapInteraction) {
                processSingleTap();
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
        // 1. If we are already scrolling fast, just slow it down to a stop then charge
        while (Mathf.Abs(scrollMechanic.Inertia) > 1.0f) {
            scrollMechanic.Inertia *= 0.5f;
        }

        // 2. Begin Charge
        isHolding = true;
        holdTimer = 0f;
        hasPlayedMaxSound = false;

        // Audio: Start Charging
        if (loopAudioSource != null && chargingSound != null) {
            loopAudioSource.clip = chargingSound;
            loopAudioSource.loop = false;
            loopAudioSource.pitch = 1.0f;
            loopAudioSource.Play();
        }
    }

    void ReleaseHold()
    {
        if (!isHolding) return;

        isHolding = false;

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
        if (isHolding && !actionRef.action.IsPressed()) {
            ReleaseHold();
        }

        if (isHolding)
        {
            holdTimer += Time.deltaTime;
            float ratio = Mathf.Clamp01(holdTimer / maxHoldDuration);

            // --- 1. VISUALS: Zoom & Shake ---
            if (scrollMechanic.camera != null)
            {
                // Zoom In (Lerp Ortho Size)
                float targetSize = defaultOrthoSize * zoomAmount;
                scrollMechanic.camera.orthographicSize = Mathf.Lerp(defaultOrthoSize, targetSize, ratio);

                // Shake (Increases with ratio)
                float currentShake = shakeIntensity * ratio * 0.1f; 
                // Shake Camera Position (Or you could shake the Canvas RectTransform)
                Vector3 shakeOffset = (Vector3)UnityEngine.Random.insideUnitCircle * currentShake;
                scrollMechanic.camera.transform.position = originalCamPos + shakeOffset;
            }

            // --- 2. AUDIO: Pitch Ramping and Custom looping ---
            if (loopAudioSource != null && loopAudioSource.isPlaying)
            {
                // Ramp pitch from 1.0 to maxChargePitch
                loopAudioSource.pitch = Mathf.Lerp(1.0f, maxChargePitch, ratio);
                // CUSTOM LOOP CHECK:
                // If we are near the end of the clip (within 0.05s margin), jump back by loopDuration
                if (loopAudioSource.time >= chargingSound.length - 0.05f)
                {
                    // Ensure we don't jump back to negative time
                    float jumpBackPoint = Mathf.Max(0, chargingSound.length - finalLoopDuration);
                    loopAudioSource.time = jumpBackPoint;
                }
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

    void ResetVisuals()
    {
        if (scrollMechanic.camera != null) {
            scrollMechanic.camera.orthographicSize = defaultOrthoSize;
            scrollMechanic.camera.transform.position = originalCamPos;
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
            currentSpeed += tapSpeedUpAmount;
            if (audioSource != null) {
                audioSource.pitch = speedUpPitch;
                audioSource.PlayOneShot(speedUpSound, speedUpVolume);
            }
        } 
        else {
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
