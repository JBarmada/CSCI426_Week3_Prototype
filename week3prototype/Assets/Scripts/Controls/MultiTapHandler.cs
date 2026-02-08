using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Mechanics; // Access PostInfo

public class MultiTapHandler : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference actionRef; // Assign your "MultiTap" action here

    [Header("Dependencies")]
    public ScrollMechanic scrollMechanic;

    [Header("Game Settings")]
    [Tooltip("Multiplier applied to inertia on tap (e.g. 0.95 = 5% slowdown)")]
    public float slowDownAmount = 0.95f; 
    
    [Tooltip("How long (in seconds) you can still like a Gold post after it leaves the center")]
    public float goldGracePeriod = 0.5f;

    [Header("Feedback")]
    public GameObject heartPrefab; // Assign a prefab (e.g. an Image of a heart)
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
        // Cache original canvas position for the shake effect
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

        // Track changes to center object to handle timers
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

        // --- GOLD GRACE PERIOD LOGIC ---
        // If the PREVIOUS center was Gold, and we haven't moved away from it for too long.
        // We prioritize liking that previous Gold post over the current one.
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
        // 1. Slow Scale
        scrollMechanic.Inertia *= slowDownAmount;

        // 2. Play Audio
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

        // 3. Visual FX (Heart)
        if (heartPrefab != null)
        {
            // Spawn heart at the post's world position, inside the canvas hierarchy
            Transform parent = scrollMechanic.targetCanvas.parent; // Usually main Canvas
            GameObject heart = Instantiate(heartPrefab, parent);
            heart.transform.position = info.transform.position;
            
            // Optional: Destroy heart after 1 second if the prefab doesn't handle it
            Destroy(heart, 1.0f);
        }

        // 4. Shake
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