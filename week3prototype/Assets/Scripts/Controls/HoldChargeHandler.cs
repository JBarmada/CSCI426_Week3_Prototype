using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

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
    public ScrollMechanic scrollMechanic;

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

    // --- Internal State ---
    bool isHolding;
    bool isLooping;
    bool maxSoundPlayed;
    float holdTimer;

    Vector2 originalCanvasPos;
    Vector3 originalCanvasScale;

    Coroutine releaseRoutine;

    void OnEnable() => actionRef.action.Enable();
    void OnDisable() => actionRef.action.Disable();

    void Start()
    {
        originalCanvasPos = scrollMechanic.targetCanvas.anchoredPosition;
        originalCanvasScale = scrollMechanic.targetCanvas.localScale;

        actionRef.action.performed += ctx =>
        {
            if (ctx.interaction is HoldInteraction)
            {
                Debug.Log("[Hold] Hold PERFORMED");
                StartHold();
            }
        };

        actionRef.action.canceled += _ =>
        {
            if (isHolding)
            {
                Debug.Log("[Hold] Hold RELEASED");
                ReleaseHold();
            }
        };
    }

    void StartHold()
    {
        if (releaseRoutine != null)
            StopCoroutine(releaseRoutine);

        // Reduce existing momentum before charging
        if (Mathf.Abs(scrollMechanic.Inertia) > 1f)
            scrollMechanic.Inertia *= 0.5f;

        isHolding = true;
        isLooping = false;
        maxSoundPlayed = false;
        holdTimer = 0f;

        // Play buildup audio
        loopSource.Stop();
        loopSource.clip = chargeBuildupSound;
        loopSource.loop = false;
        loopSource.pitch = 1f;
        loopSource.Play();

        Debug.Log("[Hold] Charge buildup started");
    }

    void ReleaseHold()
    {
        isHolding = false;
        isLooping = false;

        float ratio = Mathf.Clamp01(holdTimer / maxHoldDuration);

        // Cancel if barely charged
        if (ratio < 0.1f)
        {
            ResetVisuals();
            loopSource.Stop();
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

        oneShotSource.PlayOneShot(launchSound);

        float t = 0f;
        while (t < recoilDuration)
        {
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
