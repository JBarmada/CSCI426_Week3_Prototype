using TMPro;
using UnityEngine;
using System.Collections; 

public class GameWinTimer : MonoBehaviour
{
    [Header("Timer")]
    public float winTimeSeconds = 90f;

    [Header("Dependencies")]
    public DopamineManager dopamineManager;
    public SlideUpPanel winMenu;

    [Header("Win Audio")]
    public AudioSource winSfxSource;
    public AudioClip winSfxClip;
    public float winHitStopDuration = 1f;
    public float winPanelSlideDuration = 1.2f;

    [Header("Low Time Warning")]
    public AudioClip lowTimeClip;
    [Range(0f, 1f)] public float lowTimeVolume = 0.4f;
    public float lowTimeThresholdSeconds = 5f;

    [Header("UI")]
    public TextMeshProUGUI timerText;

    private float elapsed;
    private bool winTriggered;
    private AudioSource _lowTimeSource;
    private bool _lowTimePlaying;

    void Start()
    {
        elapsed = 0f;
        UpdateTimerText();

        if (lowTimeClip != null)
        {
            _lowTimeSource = gameObject.AddComponent<AudioSource>();
            _lowTimeSource.clip = lowTimeClip;
            _lowTimeSource.loop = true;
            _lowTimeSource.volume = lowTimeVolume;
            _lowTimeSource.playOnAwake = false;
        }
    }

    void Update()
    {
        if (winTriggered) return;
        if (GameMenusManager.Instance != null && GameMenusManager.Instance.IsPaused)
        {
            StopLowTimeWarning();
            return;
        }
        if (dopamineManager == null) return;

        elapsed += Time.deltaTime;
        UpdateTimerText();

        if (elapsed < winTimeSeconds)
        {
            UpdateLowTimeWarning();
            return;
        }

        if (dopamineManager.getCurrDop() > 0f)
        {
            winTriggered = true;
            StopLowTimeWarning();
            StartCoroutine(WinSequence());
        }
    }

    private IEnumerator WinSequence()
    {
        if (GameMenusManager.Instance != null)
        {
            GameMenusManager.Instance.PauseForWin();
        }

        if (winMenu != null)
        {
            winMenu.Show();
        }

        float sfxDelay = 0f;
        if (winSfxSource != null && winSfxClip != null)
        {
            winSfxSource.PlayOneShot(winSfxClip);
            sfxDelay = winSfxClip.length;
        }

        float waitDuration = Mathf.Max(winPanelSlideDuration, winHitStopDuration, sfxDelay);
        if (waitDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(waitDuration);
        }

        if (GameMenusManager.Instance != null)
        {
            GameMenusManager.Instance.PlayWinMusic();
        }
    }

    private void UpdateLowTimeWarning()
    {
        if (_lowTimeSource == null) return;

        float remaining = Mathf.Max(0f, winTimeSeconds - elapsed);
        bool shouldPlay = remaining > 0f && remaining <= lowTimeThresholdSeconds;
        if (shouldPlay && !_lowTimePlaying)
        {
            _lowTimeSource.Play();
            _lowTimePlaying = true;
        }
        else if (!shouldPlay && _lowTimePlaying)
        {
            StopLowTimeWarning();
        }
    }

    private void StopLowTimeWarning()
    {
        if (_lowTimeSource == null || !_lowTimePlaying) return;
        _lowTimeSource.Stop();
        _lowTimePlaying = false;
    }

    private void UpdateTimerText()
    {
        if (timerText == null) return;

        float remaining = Mathf.Max(0f, winTimeSeconds - elapsed);
        int minutes = Mathf.FloorToInt(remaining / 60);
        int seconds = Mathf.CeilToInt(remaining % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    } 
}
