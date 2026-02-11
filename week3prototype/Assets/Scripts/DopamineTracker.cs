using System.Collections;
using UnityEngine;

public class DopamineTracker : MonoBehaviour
{
    public DopamineManager manager;
    private float currDop;
    private bool gameNotOverBefore = false;

    public SlideUpPanel gameOverMenu; 

    [Header("Game Over Hit Stop")]
    public RectTransform dopamineBarTransform;
    public float hitStopDuration = 0.5f;
    public float barShakeIntensity = 8f;

    [Header("Game Over Audio")]
    public AudioSource gameOverSfxSource;
    public AudioClip gameOverSfxClip;
    public float gameOverPanelSlideDuration = 1.2f;
    public float gameOverMusicFadeOutDuration = 0.15f;

    [Header("Low Dopamine Warning")]
    public AudioClip lowDopamineClip;
    [Range(0f, 1f)] public float lowDopamineVolume = 0.4f;
    [Range(0f, 1f)] public float lowDopamineThresholdPercent = 0.2f;
    public float lowDopamineCooldown = 1f;

    private AudioSource _lowDopamineSource;
    private bool _lowDopaminePlaying = false;
    private float _nextLowDopamineAllowedTime = 0f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (lowDopamineClip != null)
        {
            _lowDopamineSource = gameObject.AddComponent<AudioSource>();
            _lowDopamineSource.clip = lowDopamineClip;
            _lowDopamineSource.loop = true;
            _lowDopamineSource.volume = lowDopamineVolume;
            _lowDopamineSource.playOnAwake = false;
            _lowDopamineSource.ignoreListenerPause = true; // important so hitstop doesn't mute it
        }
    }

    // Update is called once per frame
    void Update()
    {
        //keep track of the current dopamine value every half second or something 
        currDop = manager.getCurrDop();
        if (currDop <= 0 && !gameNotOverBefore)
        {
            gameNotOverBefore = true;
            StopLowDopamineWarning(false);
            StartCoroutine(GameOverSequence());
        }

        UpdateLowDopamineWarning();

    }

    private void UpdateLowDopamineWarning()
    {
        if (_lowDopamineSource == null || manager == null || gameNotOverBefore)
        {
            return;
        }

        if (GameMenusManager.Instance != null && GameMenusManager.Instance.IsPaused)
        {
            StopLowDopamineWarning(true);
            return;
        }

        float maxDop = manager.GetMaxDop();
        if (maxDop <= 0f)
        {
            return;
        }

        bool shouldPlay = currDop > 0f && (currDop / maxDop) <= lowDopamineThresholdPercent;
        if (shouldPlay && !_lowDopaminePlaying)
        {
            if (Time.unscaledTime < _nextLowDopamineAllowedTime)
            {
                return;
            }
            _lowDopamineSource.Play();
            _lowDopaminePlaying = true;
        }
        else if (!shouldPlay && _lowDopaminePlaying)
        {
            StopLowDopamineWarning(true);
        }
    }

    private void StopLowDopamineWarning(bool applyCooldown)
    {
        if (_lowDopamineSource == null || !_lowDopaminePlaying)
        {
            return;
        }

        _lowDopamineSource.Stop();
        _lowDopaminePlaying = false;

        if (applyCooldown)
        {
            _nextLowDopamineAllowedTime = Time.unscaledTime + lowDopamineCooldown;
        }
    }

    private IEnumerator GameOverSequence()
    {
        Debug.Log("GAME OVER");

        if (BackgroundMusic.Instance != null)
        {
            BackgroundMusic.Instance.FadeOut(gameOverMusicFadeOutDuration);
        }

        if (GameMenusManager.Instance != null)
        {
            GameMenusManager.Instance.PauseForGameOver();
        }
        if (gameOverMenu != null)
        {
            gameOverMenu.Show();
        }

        float sfxDelay = 0f;
        if (gameOverSfxSource != null && gameOverSfxClip != null)
        {
            gameOverSfxSource.PlayOneShot(gameOverSfxClip);
            sfxDelay = gameOverSfxClip.length;
        }

        if (dopamineBarTransform != null)
        {
            StartCoroutine(ShakeBar(hitStopDuration));
        }

        float waitDuration = Mathf.Max(gameOverPanelSlideDuration, sfxDelay, hitStopDuration);
        if (waitDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(waitDuration);
        }

        if (GameMenusManager.Instance != null)
        {
            GameMenusManager.Instance.PlayGameOverMusic();
        }
    }

    private IEnumerator ShakeBar(float duration)
    {
        Vector3 originalPos = dopamineBarTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            Vector2 offset = Random.insideUnitCircle * barShakeIntensity;
            dopamineBarTransform.localPosition = originalPos + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        dopamineBarTransform.localPosition = originalPos;
    }
}
