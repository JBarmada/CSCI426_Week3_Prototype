using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    public static BackgroundMusic Instance;

    [Header("Settings")]
    public AudioClip musicClip;
    [Range(0f, 1f)] public float volume = 0.5f;

    private AudioSource _audioSource;
    private Coroutine _activeFadeRoutine;
    private readonly List<AudioClip> _temporaryStack = new List<AudioClip>();

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

    public void PushTemporaryMusic(AudioClip clip, float duration = 1.0f)
    {
        if (_audioSource == null || clip == null) return;
        if (IsPlayingClip(clip)) return;

        _temporaryStack.Add(_audioSource.clip);
        CrossfadeMusic(clip, duration);
    }

    public void PopTemporaryMusic(AudioClip clip, float duration = 1.0f)
    {
        if (_audioSource == null || clip == null) return;

        if (IsPlayingClip(clip))
        {
            if (_temporaryStack.Count == 0) return;

            AudioClip previous = _temporaryStack[_temporaryStack.Count - 1];
            _temporaryStack.RemoveAt(_temporaryStack.Count - 1);

            if (previous != null)
            {
                CrossfadeMusic(previous, duration);
            }
            else
            {
                if (_activeFadeRoutine != null) StopCoroutine(_activeFadeRoutine);
                _audioSource.Stop();
                _audioSource.clip = null;
                _audioSource.volume = volume;
            }
        }
        else
        {
            for (int i = _temporaryStack.Count - 1; i >= 0; i--)
            {
                if (_temporaryStack[i] == clip)
                {
                    _temporaryStack.RemoveAt(i);
                    break;
                }
            }
        }
    }

    // --- NEW: Fading Logic ---

    public void FadeOut(float duration)
    {
        if (_activeFadeRoutine != null) StopCoroutine(_activeFadeRoutine);
        _activeFadeRoutine = StartCoroutine(FadeRoutine(0f, duration));
    }

    public void FadeIn(float duration)
    {
        if (_activeFadeRoutine != null) StopCoroutine(_activeFadeRoutine);
        _activeFadeRoutine = StartCoroutine(FadeRoutine(volume, duration));
    }

    public void CrossfadeMusic(AudioClip newClip, float duration = 1.0f)
    {
        if (newClip == _audioSource.clip) return; // Don't restart same song
        
        if (_activeFadeRoutine != null) StopCoroutine(_activeFadeRoutine);
        _activeFadeRoutine = StartCoroutine(CrossfadeRoutine(newClip, duration));
    }

    IEnumerator FadeRoutine(float targetVolume, float duration)
    {
        float startVol = _audioSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (_audioSource == null) yield break; // Safety check

            elapsed += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(startVol, targetVolume, elapsed / duration);
            yield return null;
        }
        if (_audioSource != null) _audioSource.volume = targetVolume;
        _activeFadeRoutine = null;
    }

    IEnumerator CrossfadeRoutine(AudioClip newClip, float duration)
    {
        float halfTime = duration / 2f;

        // 1. Fade Out
        yield return FadeRoutine(0f, halfTime);

        // 2. Swap
        if (_audioSource != null)
        {
            _audioSource.clip = newClip;
            _audioSource.Play();
        }

        // 3. Fade In
        yield return FadeRoutine(volume, halfTime);
        
        _activeFadeRoutine = null;
    }
}