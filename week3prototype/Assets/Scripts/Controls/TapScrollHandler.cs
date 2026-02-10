using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

/// <summary>
/// Handles SINGLE TAP interaction.
/// Adjusts scroll inertia based on current speed.
/// </summary>
public class TapScrollHandler : MonoBehaviour
{
    public InputActionReference actionRef;
    public NewScrollMechanic scrollMechanic;

    [Header("Tap Tuning")]
    public float tapSpeedThreshold = 5f;
    public float tapSpeedUpAmount = 0.4f;
    public float slowMultiplier = 0.6f;
    
    [Header("Tap Shake (Micro Juice)")]
    [Tooltip("How strong the tap shake is (pixels)")]
    public float tapShakeIntensity = 6f;

    [Tooltip("How long the tap shake lasts (seconds)")]
    public float tapShakeDuration = 0.08f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip speedUpSound;
    public AudioClip slowDownSound;
    [Range(0f, 1f)] public float speedUpVolume = 0.4f;
    [Range(0f, 1f)] public float slowDownVolume = 0.75f;

    // Internal
    private Vector2 originalCanvasPos;
    private Coroutine shakeRoutine;

    void OnEnable()
    {
        if (actionRef != null)
        {
            actionRef.action.Enable();
            actionRef.action.performed += OnTapPerformed;
        }
    }

    void OnDisable()
    {
        if (actionRef != null)
        {
            actionRef.action.performed -= OnTapPerformed;
            actionRef.action.Disable();
        }
    }

    void Start()
    {
         // Cache original canvas position once
        if (scrollMechanic != null && scrollMechanic.targetCanvas != null)
            originalCanvasPos = scrollMechanic.targetCanvas.anchoredPosition;
    }

    void OnTapPerformed(InputAction.CallbackContext ctx)
    {
        if (ctx.interaction is TapInteraction)
            ProcessTap();
    }

    void ProcessTap()
    {
        if (GameMenusManager.Instance != null && GameMenusManager.Instance.IsPaused) return;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        float velocity = scrollMechanic.Inertia;
        float direction = Mathf.Sign(velocity == 0 ? 1 : velocity);
        float speed = Mathf.Abs(velocity);

        bool spedUp;
        if (speed < tapSpeedThreshold)
        {
            speed += tapSpeedUpAmount;
            spedUp = true;
            if (audioSource != null && speedUpSound != null)
                audioSource.PlayOneShot(speedUpSound, speedUpVolume);
            Debug.Log("[Tap] Speed UP to " + speed);
        }
        else
        {
            speed *= slowMultiplier;
            spedUp = false;
            if (audioSource != null && slowDownSound != null)
                audioSource.PlayOneShot(slowDownSound, slowDownVolume);
            Debug.Log("[Tap] Slow DOWN to " + speed);
        }

        scrollMechanic.Inertia = speed * direction;
        // --- MICRO SHAKE ---
        TriggerTapShake(spedUp);
    }

     /// <summary>
    /// Small, quick shake to give tactile feedback on tap.
    /// Speed-up shakes slightly stronger than slow-down.
    /// </summary>
    void TriggerTapShake(bool isSpeedUp)
    {
        if (scrollMechanic == null || scrollMechanic.targetCanvas == null)
            return;

        // Stop previous shake so taps don't stack endlessly
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        float intensity = isSpeedUp
            ? tapShakeIntensity
            : tapShakeIntensity * 0.6f; // slower taps feel softer

        shakeRoutine = StartCoroutine(TapShakeRoutine(intensity));
    }

    IEnumerator TapShakeRoutine(float intensity)
    {
        float elapsed = 0f;

        while (elapsed < tapShakeDuration)
        {
            elapsed += Time.deltaTime;

            // Random 2D offset (UI shake)
            Vector2 offset = Random.insideUnitCircle * intensity;
            scrollMechanic.targetCanvas.anchoredPosition = originalCanvasPos + offset;

            yield return null;
        }

        // Snap back cleanly
        scrollMechanic.targetCanvas.anchoredPosition = originalCanvasPos;
        shakeRoutine = null;
    }
}
