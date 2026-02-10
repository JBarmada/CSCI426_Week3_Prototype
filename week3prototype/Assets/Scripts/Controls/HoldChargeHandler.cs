using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using Mechanics;

/// <summary>
/// Handles HOLD interaction:
/// - Charge buildup
/// - Charge audio (buildup → loop)
/// - UI zoom + shake while holding
/// - Release pause, launch impulse, recoil
/// </summary>
public class HoldChargeHandler : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference actionRef;

    [Header("Dependencies")]
    public NewScrollMechanic scrollMechanic;
    public TapScrollHandler tapScrollHandler; // NEW: Assign this in Inspector!


    [Header("Charge Settings")]
    public float maxHoldDuration = 2f;
    public float minHoldPower = 10f;
    public float maxHoldPower = 80f;

    [Header("Visual Juice")]
    public float shakeIntensity = 15f;
    public float zoomAmount = 1.1f;

    [Header("Release Feel")]
    public float preLaunchPause = 0.35f;
    public float recoilDuration = 1.2f;
    public float recoilIntensity = 70f;

    [Header("Audio")]
    public AudioSource oneShotSource;
    public AudioSource loopSource;

    public AudioClip chargeBuildupSound;
    public AudioClip chargeLoopSound;
    public AudioClip maxChargeSound;
    public AudioClip launchSound;

    [Range(1f, 3f)] public float maxChargePitch = 2f;
    [Header("Music Switch")]
    public AudioClip funMusicClip; // Assign "Fun" music here
    [Header("Gameplay Evolution")]
    public bool enableEvolution = true; // Flag to enable/disable this feature
    public float evolvedInertiaSense = 0.35f; // New friction (very slippery)
    public float evolvedTapPush = 4f; 

    [Header("Hyperdrive FX")]
    public GameObject hyperdrivePrefab;
    public float hyperdriveSpeedThreshold = 30f; // Speed at which the effect cuts off

     [Header("Screen Flip")]
    public ScreenFlipper screenFlipper; // Assign new script here
    private int fullChargeCount = 0;
    [Header("Bonus Games")]
    public SpaceInvaderMiniGame spaceInvaderGame; // NEW: Assign in inspector
    public float flipstoSpawnInvaders = 2f; // How many flips before spawning invaders (e.g. 3 = on 6th charge)
    public bool bonusGameActive = false; // Flag to control if bonus game can be activated
    [Header("Post Specials")]
    // [Range(0f, 1f)]
    // public float spaceInvaderConversionRate = 0.05f; // Moved to PostFXManager


    // --- Internal State ---
    bool isHolding;
    bool isLooping;
    bool maxSoundPlayed;
    float holdTimer;
    GameObject activeHyperdrive;
    bool hasEvolved; // Track if we've already loosened the game
    private int flipEventCount = 0; // NEW: Track how many flips have happened


    Vector2 originalCanvasPos;
    Vector3 originalCanvasScale;

    Coroutine releaseRoutine;
    Coroutine fadeCheckRoutine; // NEW: To handle delayed fade

    bool launchLoopWasPlaying;


    void OnEnable()
    {
        if (actionRef != null)
        {
            actionRef.action.Enable();
            actionRef.action.performed += OnHoldPerformed;
            actionRef.action.canceled += OnHoldCanceled;
        }
    }

    void OnDisable()
    {
        if (actionRef != null)
        {
            actionRef.action.performed -= OnHoldPerformed;
            actionRef.action.canceled -= OnHoldCanceled;
            actionRef.action.Disable();
        }
    }

    void Start()
    {
        originalCanvasPos = scrollMechanic.targetCanvas.anchoredPosition;
        originalCanvasScale = scrollMechanic.targetCanvas.localScale;

    }

    void OnHoldPerformed(InputAction.CallbackContext ctx)
    {
        if (ctx.interaction is HoldInteraction)
        {
            Debug.Log("[Hold] Hold PERFORMED");
            StartHold();
        }
    }

    void OnHoldCanceled(InputAction.CallbackContext ctx)
    {
        if (isHolding)
        {
            Debug.Log("[Hold] Hold RELEASED");
            ReleaseHold();
        }
    }

    void StartHold()
    {
        if (GameMenusManager.Instance != null && GameMenusManager.Instance.IsPaused) return;

        if (releaseRoutine != null)
            StopCoroutine(releaseRoutine);

        if (fadeCheckRoutine != null) StopCoroutine(fadeCheckRoutine);


        // Stop hyperdrive if we grab the screen
        if (activeHyperdrive != null)
        {
            Destroy(activeHyperdrive);
            activeHyperdrive = null;
        }

        // Reduce existing momentum before charging
        if (Mathf.Abs(scrollMechanic.Inertia) > 1f)
            scrollMechanic.Inertia *= 0.5f;

        isHolding = true;
        isLooping = false;
        maxSoundPlayed = false;
        holdTimer = 0f;
        // FADE OUT CURRENT MUSIC
        fadeCheckRoutine = StartCoroutine(CheckMusicFade());
        
        // Play buildup audio
        loopSource.Stop();
        loopSource.clip = chargeBuildupSound;
        loopSource.loop = false;
        loopSource.pitch = 1f;
        loopSource.Play();

        Debug.Log("[Hold] Charge buildup started");
    }
    IEnumerator CheckMusicFade()
    {
        // Don't fade logic if "Fun Music" is already playing!
        if (BackgroundMusic.Instance != null && BackgroundMusic.Instance.IsPlayingClip(funMusicClip))
        {
            yield break;
        }

        // Wait 1.5 seconds before fading out background music
        yield return new WaitForSeconds(2.5f);

        if (isHolding && BackgroundMusic.Instance != null)
        {
            BackgroundMusic.Instance.FadeOut(0.5f);
        }
    }


    void ReleaseHold()
    {
        if (GameMenusManager.Instance != null && GameMenusManager.Instance.IsPaused) return;

        isHolding = false;
        isLooping = false;
        if (fadeCheckRoutine != null) 
        {
            StopCoroutine(fadeCheckRoutine);
            fadeCheckRoutine = null;
        }

        float ratio = Mathf.Clamp01(holdTimer / maxHoldDuration);

        // Cancel if barely charged
        if (ratio < 0.1f)
        {
            ResetVisuals();
            loopSource.Stop();
            // FIX: If we faded out but cancelled, restore volume immediately
            if (BackgroundMusic.Instance != null)
                 BackgroundMusic.Instance.FadeIn(0.2f);
            
            return;
        }

        releaseRoutine = StartCoroutine(ReleaseSequence(ratio));
    }

    IEnumerator ReleaseSequence(float ratio)
    {
        loopSource.Stop();
        yield return new WaitForSeconds(preLaunchPause);
        

        float launchVelocity = Mathf.Lerp(minHoldPower, maxHoldPower, ratio);
        scrollMechanic.Inertia = launchVelocity;
        bool isHyperdrive = false;
        // Trigger Hyperdrive if valid and near max power (>95%)
        if (hyperdrivePrefab != null && ratio >= 0.95f)
        {
            // Increment Count
            fullChargeCount++;

            // CHECK EVEN NUMBER (2, 4, 6...)
            
            if (screenFlipper != null && fullChargeCount % 1 == 0)
            {
                 // MUSIC SWAP LOGIC
                if (BackgroundMusic.Instance != null)
                {
                    // If we are already playing fun music, keep it playing
                    bool isAlreadyFun = BackgroundMusic.Instance.IsPlayingClip(funMusicClip);

                    // If launch is strong (>50%) AND we aren't already playing fun music
                    if (ratio > 0.5f && funMusicClip != null && !isAlreadyFun)
                    {
                        BackgroundMusic.Instance.CrossfadeMusic(funMusicClip, 0.2f);
                    }
                    else
                    {
                        // Otherwise just restore volume (in case we faded out)
                        BackgroundMusic.Instance.FadeIn(0.5f);
                    }
                }
                screenFlipper.DoFlip();
                
                // Convert posts based on PostFXManager rules
                if (scrollMechanic != null && PostFXManager.Instance != null && PostFXManager.Instance.conversionRules != null)
                {
                    foreach (var rule in PostFXManager.Instance.conversionRules)
                    {
                        if (rule.addOnFlip && rule.flipRatio > 0f)
                        {
                            scrollMechanic.ConvertTypeToSpecial(rule.targetTypeToConvert, rule.specialType, rule.flipRatio);
                        }
                    }
                }

                flipEventCount++; 
                if (enableEvolution && !hasEvolved)
                {
                    hasEvolved = true;

                    // 1. Loosen Friction (Scroll Mechanic)
                    if (scrollMechanic != null)
                    {
                        scrollMechanic.inertiaSense = evolvedInertiaSense;
                    }

                    // 2. Boost Tap Speed (Tap Handler)
                    if (tapScrollHandler != null)
                    {
                        tapScrollHandler.tapSpeedUpAmount = evolvedTapPush;
                    }

                    Debug.Log($"[Gameplay] EVOLVED! InertiaSense: {evolvedInertiaSense}, TapPush: {evolvedTapPush}");
                }


                // TRIGGER SPACE INVADERS ON x FLIP (Charge #6)
                
                if (flipEventCount >= flipstoSpawnInvaders && spaceInvaderGame != null)
                 {
                    if (bonusGameActive)
                    {
                        spaceInvaderGame.ActivateGame();

                    }
                 }
            }
            
            if (activeHyperdrive != null) Destroy(activeHyperdrive);
            
            // 1. Instantiate (Like PostFXManager)
            activeHyperdrive = Instantiate(hyperdrivePrefab, scrollMechanic.targetCanvas);
            
            // 2. Reset Transform Basics
            activeHyperdrive.transform.localPosition = new Vector3(0, 0, -500f); // Pull it CLOSER to camera
            activeHyperdrive.transform.localRotation = Quaternion.identity;
            activeHyperdrive.transform.localScale = Vector3.one; 

            // 3. Handle UI RectTransform stretching
            RectTransform rt = activeHyperdrive.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero; // Bottom-Left
                rt.anchorMax = Vector2.one;  // Top-Right
                rt.offsetMin = Vector2.zero; 
                rt.offsetMax = Vector2.zero; 
            }

            // 4. CRITICAL: Force Draw Order
            activeHyperdrive.transform.SetAsLastSibling(); 
            
            // 5. CRITICAL: Fix Particle System Scaling
            // If the prefab has particles, they might be tiny in UI coordinates (pixels).
            // You might need to scale them up significantly if they look small.
            var particles = activeHyperdrive.GetComponentsInChildren<ParticleSystem>();
            foreach(var p in particles) {
                var main = p.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy; 
                main.simulationSpeed = 10f;
            }
            isHyperdrive = true;
        }

        //Debug.Break();
        if (GameMenusManager.Instance != null && GameMenusManager.Instance.IsPaused)
        {
            yield return null;
            while (GameMenusManager.Instance != null && GameMenusManager.Instance.IsPaused)
                yield return null;
        }

        if (isHyperdrive)
        {
            // Play looping launch sound
            loopSource.clip = launchSound;
            loopSource.loop = true;
            loopSource.pitch = 1f;
            loopSource.Play();
        }
        else
        {
            // Standard one-shot
            oneShotSource.PlayOneShot(launchSound);
        }

        float t = 0f;
        while (t < recoilDuration)
        {
            if (GameMenusManager.Instance != null && GameMenusManager.Instance.IsPaused)
            {
                yield return null;
                continue;
            }

            t += Time.deltaTime;
            float decay = 1f - (t / recoilDuration);

            Vector2 shake = Random.insideUnitCircle * recoilIntensity * decay;
            scrollMechanic.targetCanvas.anchoredPosition = originalCanvasPos + shake;

            scrollMechanic.targetCanvas.localScale =
                Vector3.Lerp(scrollMechanic.targetCanvas.localScale, originalCanvasScale, t / recoilDuration);

            yield return null;
        }

        ResetVisuals();
    }

    void Update()
    {   
        if (GameMenusManager.Instance != null && GameMenusManager.Instance.IsPaused)
        {
            if (loopSource != null && loopSource.isPlaying && loopSource.clip == launchSound)
            {
                launchLoopWasPlaying = true;
                loopSource.Stop();
                loopSource.loop = false;
            }
            return;
        }

        if (launchLoopWasPlaying && loopSource != null && loopSource.clip == launchSound)
        {
            launchLoopWasPlaying = false;
            loopSource.loop = true;
            loopSource.Play();
        }

        // Monitor Hyperdrive: Stop effect if speed drops below threshold
        if (activeHyperdrive != null)
        {
            if (Mathf.Abs(scrollMechanic.Inertia) < hyperdriveSpeedThreshold)
            {
                Destroy(activeHyperdrive);
                activeHyperdrive = null;

                // Stop audio if it's still playing the launch loop
                if (loopSource.isPlaying && loopSource.clip == launchSound)
                {
                    loopSource.Stop();
                }
            }
        }
        if (!isHolding) return;

        holdTimer += Time.deltaTime;
        float ratio = Mathf.Clamp01(holdTimer / maxHoldDuration);

        // --- Visuals ---
        Vector3 targetScale = originalCanvasScale * zoomAmount;
        scrollMechanic.targetCanvas.localScale =
            Vector3.Lerp(originalCanvasScale, targetScale, ratio);

        Vector2 shake = Random.insideUnitCircle * shakeIntensity * ratio;
        scrollMechanic.targetCanvas.anchoredPosition = originalCanvasPos + shake;

        // --- Audio ---
        if (loopSource.isPlaying)
            loopSource.pitch = Mathf.Lerp(1f, maxChargePitch, ratio);

        // Switch from buildup → loop
        if (!isLooping &&
            loopSource.clip == chargeBuildupSound &&
            !loopSource.isPlaying)
        {
            Debug.Log("[Hold] Switching to LOOP");
            isLooping = true;

            loopSource.clip = chargeLoopSound;
            loopSource.loop = true;
            loopSource.Play();
        }

        if (ratio >= 1f && !maxSoundPlayed)
        {
            maxSoundPlayed = true;
            oneShotSource.PlayOneShot(maxChargeSound);
        }
    }

    void ResetVisuals()
    {
        scrollMechanic.targetCanvas.anchoredPosition = originalCanvasPos;
        scrollMechanic.targetCanvas.localScale = originalCanvasScale;
    }
}
