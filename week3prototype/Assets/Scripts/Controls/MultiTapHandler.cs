using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using Mechanics; // Access PostInfo

public class MultiTapHandler : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference actionRef; 

    [Header("Dependencies")]
    public NewScrollMechanic scrollMechanic;

    [Header("Game Settings")]
    [Tooltip("Multiplier applied to inertia on tap (e.g. 0.95 = 5% slowdown)")]
    public float SpeedFactor = 0.95f; 
    
    [Tooltip("How long (in seconds) you can still like a Gold post after it leaves the center")]
    public float goldGracePeriod = 0.5f;

    [Header("Feedback")]
    public GameObject heartPrefab; 
    public AudioSource audioSource;
    public AudioClip likeSound_Neu;
    public AudioClip likeSound_Pos;
    public AudioClip likeSound_Neg;
    public AudioClip likeSound_Gold;

    [Header("Shake Settings")]
    public float shakeIntensity = 6f;
    public float shakeDuration = 0.08f;

    // State
    private GameObject lastCenterObj;
    private float timeSinceLastChange;

    // Shake internal
    private Vector2 originalCanvasPos;
    private Coroutine shakeRoutine;

    private void Start()
    {
        if (scrollMechanic != null && scrollMechanic.targetCanvas != null)
            originalCanvasPos = scrollMechanic.targetCanvas.anchoredPosition;

        actionRef.action.performed += ctx =>
        {
            if (ctx.interaction is MultiTapInteraction)
                DetermineAndLikeTarget();
        };
    }

    void OnEnable() => actionRef.action.Enable();
    void OnDisable() => actionRef.action.Disable();

    private void Update()
    {
        if (scrollMechanic == null) return;

        GameObject current = scrollMechanic.GetCurrentCenterObject();
        if (current != lastCenterObj)
        {
            lastCenterObj = current;
            timeSinceLastChange = 0f;
        }
        else
        {
            timeSinceLastChange += Time.deltaTime;
        }
    }

    private void DetermineAndLikeTarget()
    {
        GameObject currentCenter = scrollMechanic.GetCurrentCenterObject();
        GameObject target = currentCenter;

        if (lastCenterObj != null && lastCenterObj != currentCenter && timeSinceLastChange < goldGracePeriod)
        {
            PostInfo lastInfo = lastCenterObj.GetComponent<PostInfo>();
            if (lastInfo != null && lastInfo.currentType == PostInfo.PostType.Gold)
            {
                target = lastCenterObj;
                Debug.Log("Gold Grace Period Active! Liking previous Gold post.");
            }
        }

        if (target != null)
        {
            PostInfo info = target.GetComponent<PostInfo>();
            if (info != null)
            {
                ApplyEffects(info);
            }
        }
    }

    private void ApplyEffects(PostInfo info)
    {
        // 1. Update Stats (NEW)
        if (GameStatsManager.Instance != null)
        {
            GameStatsManager.Instance.TrackLike(info.currentType);
        }

        // 2. Slow Scale
        scrollMechanic.Inertia *= SpeedFactor;

        // 3. Play Audio
        if (audioSource != null)
        {
            AudioClip clip = null;
            switch (info.currentType)
            {
                case PostInfo.PostType.Neutral: clip = likeSound_Neu; break;
                case PostInfo.PostType.Positive: clip = likeSound_Pos; break;
                case PostInfo.PostType.Negative: clip = likeSound_Neg; break;
                case PostInfo.PostType.Gold: clip = likeSound_Gold; break;
            }
            if (clip != null) audioSource.PlayOneShot(clip);
        }

        // 4. Visual FX (Heart)
        if (heartPrefab != null)
        {
            Instantiate(heartPrefab, info.transform);
        }

        // 5. Shake
        TriggerTapShake();
        
        Debug.Log($"Liked Post Type: {info.currentType}");
    }

    // --- Shake Logic ---
    void TriggerTapShake()
    {
        if (scrollMechanic.targetCanvas == null) return;

        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(TapShakeRoutine(shakeIntensity));
    }

    IEnumerator TapShakeRoutine(float intensity)
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            Vector2 offset = Random.insideUnitCircle * intensity;
            scrollMechanic.targetCanvas.anchoredPosition = originalCanvasPos + offset;
            yield return null;
        }
        scrollMechanic.targetCanvas.anchoredPosition = originalCanvasPos;
        shakeRoutine = null;
    }
}