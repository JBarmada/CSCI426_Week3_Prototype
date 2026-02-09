using System.Collections;
using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    public static BackgroundMusic Instance;

    [Header("Settings")]
    public AudioClip musicClip;
    [Range(0f, 1f)] public float volume = 0.5f;

    private AudioSource _audioSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
        
        PlayMusic();
    }

    void PlayMusic()
    {
        if (musicClip == null) return;
        _audioSource.clip = musicClip;
        _audioSource.volume = volume;
        _audioSource.loop = true;
        _audioSource.playOnAwake = true;
        _audioSource.Play();
    }

    public void SetVolume(float vol)
    {
        volume = vol;
        if (_audioSource != null) _audioSource.volume = volume;
    }

    public AudioClip CurrentClip
    {
        get { return _audioSource != null ? _audioSource.clip : null; }
    }

     // NEW HELPER
    public bool IsPlayingClip(AudioClip clip)
    {
        if (_audioSource == null || clip == null) return false;
        return _audioSource.clip == clip;
    }

    // --- NEW: Fading Logic ---

    public void FadeOut(float duration)
    {
        StartCoroutine(FadeRoutine(0f, duration));
    }

    public void FadeIn(float duration)
    {
        StartCoroutine(FadeRoutine(volume, duration));
    }

    public void CrossfadeMusic(AudioClip newClip, float duration = 1.0f)
    {
        if (newClip == _audioSource.clip) return; // Don't restart same song
        StartCoroutine(CrossfadeRoutine(newClip, duration));
    }

    IEnumerator FadeRoutine(float targetVolume, float duration)
    {
        float startVol = _audioSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(startVol, targetVolume, elapsed / duration);
            yield return null;
        }
        _audioSource.volume = targetVolume;
    }

    IEnumerator CrossfadeRoutine(AudioClip newClip, float duration)
    {
        float halfTime = duration / 2f;

        // 1. Fade Out
        yield return FadeRoutine(0f, halfTime);

        // 2. Swap
        _audioSource.clip = newClip;
        _audioSource.Play();

        // 3. Fade In
        yield return FadeRoutine(volume, halfTime);
    }
}