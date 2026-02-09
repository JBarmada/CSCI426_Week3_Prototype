using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using Mechanics; 

public class MultiTapHandler : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference actionRef; 

    [Header("Dependencies")]
    public NewScrollMechanic scrollMechanic;
    public SpaceInvaderMiniGame spaceInvaderGame;

    [Header("Game Settings")]
    [Tooltip("Multiplier applied to inertia on tap (e.g. 0.95 = 5% slowdown)")]
    public float SpeedFactor = 0.95f; 
    
    [Tooltip("How long (in seconds) you can still like a Gold post after it leaves the center")]
    public float goldGracePeriod = 0.5f;

    [Tooltip("Strength of the push to the next post")]
    public float pushForce = 2f;

    [Header("Feedback")]
    public GameObject heartPrefab; // Keep this for universal feedback
    public AudioSource audioSource;
    public AudioClip likeSound_Neu;
    public AudioClip likeSound_Pos;
    public AudioClip likeSound_Neg;
    public AudioClip likeSound_Gold;
    public AudioClip likeSound_SpaceInvader;

    [Header("Space Invader Music")]
    public AudioClip spaceInvaderMusicClip;
    public float spaceInvaderMusicFadeDuration = 0.5f;

    [Header("Shake Settings")]
    public float shakeIntensity = 6f;
    public float shakeDuration = 0.08f;

    // State
    private GameObject lastCenterObj;
    private float timeSinceLastChange;
    private Vector2 originalCanvasPos;
    private Coroutine shakeRoutine;
    private bool invaderMusicActive;
    private AudioClip priorMusicClip;

    //dopamine manager testing 
    DopamineManager dopInstance;

    private void Start()
    {
        //dopamine manager testing 
        dopInstance = DopamineManager.Instance;
        if (dopInstance != null)
        {
            dopInstance.Start();
        }


        if (scrollMechanic != null && scrollMechanic.targetCanvas != null)
            originalCanvasPos = scrollMechanic.targetCanvas.anchoredPosition;

        actionRef.action.performed += ctx =>
        {
            if (ctx.interaction is MultiTapInteraction)
                DetermineAndLikeTarget();
        };
    }

    private void Update()
    {
        if (GameMenusManager.Instance != null && GameMenusManager.Instance.IsPaused) return;
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

    void OnEnable()
    {
        actionRef.action.Enable();
        RegisterInvaderGameEvents(true);
    }

    void OnDisable()
    {
        actionRef.action.Disable();
        RegisterInvaderGameEvents(false);
    }

    private void DetermineAndLikeTarget()
    {
        if (GameMenusManager.Instance != null && GameMenusManager.Instance.IsPaused) return;

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
        // 1. Update Stats 
        if (GameStatsManager.Instance != null)
        {
            GameStatsManager.Instance.TrackLike(info.currentType);
        }

        // 2. Trigger SPECIAL VISUAL FX (Manager) -----
        if (PostFXManager.Instance != null)
        {
            PostFXManager.Instance.TriggerEffect(info.currentType, info.transform);
            PostFXManager.Instance.TriggerSpecialEffect(info.currentSpecial, info.transform);
        }
        // ------------------------------------------------

        if (info.currentSpecial == PostInfo.PostSpecial.SpaceInvader)
        {
            StartSpaceInvaderGame();
        }

        // 3. Force Move Logic
        if (Mathf.Abs(scrollMechanic.Inertia) < 5f)
        {
            scrollMechanic.Inertia += pushForce; // Push forward on tap
        }
        
        scrollMechanic.Inertia *= SpeedFactor;

        // 4. Play Audio
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
            if (info.currentSpecial == PostInfo.PostSpecial.SpaceInvader && likeSound_SpaceInvader != null)
            {
                clip = likeSound_SpaceInvader;
            }
            if (clip != null) audioSource.PlayOneShot(clip);
        }

        // 5. Universal Heart Feedback
        if (heartPrefab != null)
        {
            Instantiate(heartPrefab, info.transform);
        }

        // 6. Shake
        TriggerTapShake();

        //attempt to get dopamine bar change 
        if(dopInstance != null)
        {
            dopInstance.TrackDopEffects(info.currentType);
        }
        
        
        Debug.Log($"Liked Post Type: {info.currentType}");
    }

    private void RegisterInvaderGameEvents(bool register)
    {
        if (spaceInvaderGame == null) return;

        if (register)
        {
            spaceInvaderGame.OnGameStarted += HandleInvaderGameStarted;
            spaceInvaderGame.OnGameEnded += HandleInvaderGameEnded;
        }
        else
        {
            spaceInvaderGame.OnGameStarted -= HandleInvaderGameStarted;
            spaceInvaderGame.OnGameEnded -= HandleInvaderGameEnded;
        }
    }

    private void StartSpaceInvaderGame()
    {
        if (spaceInvaderGame == null) return;
        spaceInvaderGame.ActivateGame();
    }

    private void HandleInvaderGameStarted()
    {
        if (BackgroundMusic.Instance == null || spaceInvaderMusicClip == null) return;
        if (BackgroundMusic.Instance.IsPlayingClip(spaceInvaderMusicClip))
        {
            invaderMusicActive = true;
            return;
        }

        priorMusicClip = BackgroundMusic.Instance.CurrentClip;
        invaderMusicActive = true;
        BackgroundMusic.Instance.CrossfadeMusic(spaceInvaderMusicClip, spaceInvaderMusicFadeDuration);
    }

    private void HandleInvaderGameEnded(bool win)
    {
        if (!invaderMusicActive) return;
        invaderMusicActive = false;

        if (BackgroundMusic.Instance == null) return;

        if (priorMusicClip != null)
        {
            BackgroundMusic.Instance.CrossfadeMusic(priorMusicClip, spaceInvaderMusicFadeDuration);
        }
        else
        {
            BackgroundMusic.Instance.FadeIn(spaceInvaderMusicFadeDuration);
        }

        priorMusicClip = null;
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