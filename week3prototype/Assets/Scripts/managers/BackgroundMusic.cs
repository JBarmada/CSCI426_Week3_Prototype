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
    private readonly List<TemporaryMusicState> _temporaryStack = new List<TemporaryMusicState>();
    private readonly List<float> _pitchStack = new List<float>();

    private struct TemporaryMusicState
    {
        public AudioClip clip;
        public float time;
    }

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

    public void SetPitch(float pitch)
    {
        if (_audioSource != null) _audioSource.pitch = pitch;
    }

    public void PushPitch(float pitch)
    {
        if (_audioSource == null) return;
        _pitchStack.Add(_audioSource.pitch);
        _audioSource.pitch = pitch;
    }

    public void PopPitch()
    {
        if (_audioSource == null) return;
        if (_pitchStack.Count == 0) return;

        float previous = _pitchStack[_pitchStack.Count - 1];
        _pitchStack.RemoveAt(_pitchStack.Count - 1);
        _audioSource.pitch = previous;
    }

    public AudioClip CurrentClip
    {
        get { return _audioSource != null ? _audioSource.clip : null; }
    }

    public float CurrentPitch
    {
        get { return _audioSource != null ? _audioSource.pitch : 1f; }
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

        _temporaryStack.Add(new TemporaryMusicState
        {
            clip = _audioSource.clip,
            time = _audioSource.clip != null ? _audioSource.time : 0f
        });
        CrossfadeMusic(clip, duration);
    }

    public void PopTemporaryMusic(AudioClip clip, float duration = 1.0f)
    {
        if (_audioSource == null || clip == null) return;

        if (IsPlayingClip(clip))
        {
            if (_temporaryStack.Count == 0) return;

            TemporaryMusicState previous = _temporaryStack[_temporaryStack.Count - 1];
            _temporaryStack.RemoveAt(_temporaryStack.Count - 1);

            if (previous.clip != null)
            {
                StartCrossfade(previous.clip, duration, previous.time);
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
                if (_temporaryStack[i].clip == clip)
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
        StartCrossfade(newClip, duration, 0f);
    }

    private void StartCrossfade(AudioClip newClip, float duration, float startTime)
    {
        if (_activeFadeRoutine != null) StopCoroutine(_activeFadeRoutine);
        _activeFadeRoutine = StartCoroutine(CrossfadeRoutine(newClip, duration, startTime));
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

    IEnumerator CrossfadeRoutine(AudioClip newClip, float duration, float startTime)
    {
        float halfTime = duration / 2f;

        // 1. Fade Out
        yield return FadeRoutine(0f, halfTime);

        // 2. Swap
        if (_audioSource != null)
        {
            _audioSource.clip = newClip;
            if (newClip != null)
            {
                float clampedTime = Mathf.Clamp(startTime, 0f, newClip.length - 0.01f);
                _audioSource.time = clampedTime;
            }
            _audioSource.Play();
        }

        // 3. Fade In
        yield return FadeRoutine(volume, halfTime);
        
        _activeFadeRoutine = null;
    }
}