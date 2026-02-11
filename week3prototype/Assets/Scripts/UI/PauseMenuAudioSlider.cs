using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenuAudioSlider : MonoBehaviour
{
    private const string MusicVolumeKey = "Audio.MusicVolume";
    private const string ScrollVolumeKey = "Audio.ScrollVolume";
    private const string EffectsVolumeKey = "Audio.EffectsVolume";
    private const string InteractionVolumeKey = "Audio.InteractionVolume";

    [Header("UI")]
    public Slider volumeSlider;
    public Slider musicSlider;
    public Slider scrollSlider;
    public Slider effectsSlider;
    public Slider interactionSlider;

    [Header("Audio Sources")]
    public List<AudioSource> scrollSources = new List<AudioSource>();
    public List<AudioSource> effectsSources = new List<AudioSource>();
    public List<AudioSource> interactionSources = new List<AudioSource>();

    [Header("Settings")]
    public bool syncOnEnable = true;

    private readonly Dictionary<AudioSource, float> scrollBaseVolumes = new Dictionary<AudioSource, float>();
    private readonly Dictionary<AudioSource, float> effectsBaseVolumes = new Dictionary<AudioSource, float>();
    private readonly Dictionary<AudioSource, float> interactionBaseVolumes = new Dictionary<AudioSource, float>();
    private float musicBaseVolume = 1f;

    private UnityEngine.Events.UnityAction<float> scrollHandler;
    private UnityEngine.Events.UnityAction<float> effectsHandler;
    private UnityEngine.Events.UnityAction<float> interactionHandler;

    void Awake()
    {
        scrollHandler = value =>
        {
            ApplyCategoryVolume(value, scrollSources, scrollBaseVolumes);
            SaveFloat(ScrollVolumeKey, value);
        };
        effectsHandler = value =>
        {
            ApplyCategoryVolume(value, effectsSources, effectsBaseVolumes);
            SaveFloat(EffectsVolumeKey, value);
        };
        interactionHandler = value =>
        {
            ApplyCategoryVolume(value, interactionSources, interactionBaseVolumes);
            SaveFloat(InteractionVolumeKey, value);
        };

        CacheBaseVolumes(scrollSources, scrollBaseVolumes);
        CacheBaseVolumes(effectsSources, effectsBaseVolumes);
        CacheBaseVolumes(interactionSources, interactionBaseVolumes);

        if (BackgroundMusic.Instance != null)
        {
            float savedMusicValue = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
            if (PlayerPrefs.HasKey(MusicVolumeKey) && savedMusicValue > 0f)
            {
                musicBaseVolume = BackgroundMusic.Instance.volume / savedMusicValue;
            }
            else
            {
                musicBaseVolume = BackgroundMusic.Instance.volume;
            }
        }

        ApplySavedValues();
    }

    void OnEnable()
    {
        BindSlider(volumeSlider, HandleMusicChanged);
        BindSlider(musicSlider, HandleMusicChanged);
        BindSlider(scrollSlider, scrollHandler);
        BindSlider(effectsSlider, effectsHandler);
        BindSlider(interactionSlider, interactionHandler);

        if (syncOnEnable)
        {
            SyncFromSources();
        }
    }

    void OnDisable()
    {
        UnbindSlider(volumeSlider, HandleMusicChanged);
        UnbindSlider(musicSlider, HandleMusicChanged);
        UnbindSlider(scrollSlider, scrollHandler);
        UnbindSlider(effectsSlider, effectsHandler);
        UnbindSlider(interactionSlider, interactionHandler);
    }

    private void SyncFromSources()
    {
        if (BackgroundMusic.Instance != null)
        {
            float value = musicBaseVolume > 0f ? BackgroundMusic.Instance.volume / musicBaseVolume : 1f;
            SetSliderValue(volumeSlider, value);
            SetSliderValue(musicSlider, value);
        }

        SetSliderValue(scrollSlider, GetCategoryValue(scrollSources, scrollBaseVolumes));
        SetSliderValue(effectsSlider, GetCategoryValue(effectsSources, effectsBaseVolumes));
        SetSliderValue(interactionSlider, GetCategoryValue(interactionSources, interactionBaseVolumes));
    }

    private void HandleMusicChanged(float value)
    {
        if (BackgroundMusic.Instance == null) return;
        float clamped = Mathf.Clamp01(value);
        BackgroundMusic.Instance.SetVolume(musicBaseVolume * clamped);
        SetSliderValue(volumeSlider, clamped);
        SetSliderValue(musicSlider, clamped);
        SaveFloat(MusicVolumeKey, clamped);
    }

    private void ApplyCategoryVolume(float value, List<AudioSource> sources, Dictionary<AudioSource, float> baseVolumes)
    {
        float clamped = Mathf.Clamp01(value);
        if (sources == null) return;

        for (int i = 0; i < sources.Count; i++)
        {
            AudioSource source = sources[i];
            if (source == null) continue;

            if (!baseVolumes.TryGetValue(source, out float baseVolume))
            {
                baseVolume = source.volume;
                baseVolumes[source] = baseVolume;
            }

            source.volume = baseVolume * clamped;
        }
    }

    private void ApplySavedValues()
    {
        float savedMusic = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        float savedScroll = PlayerPrefs.GetFloat(ScrollVolumeKey, 1f);
        float savedEffects = PlayerPrefs.GetFloat(EffectsVolumeKey, 1f);
        float savedInteraction = PlayerPrefs.GetFloat(InteractionVolumeKey, 1f);

        if (BackgroundMusic.Instance != null)
        {
            BackgroundMusic.Instance.SetVolume(musicBaseVolume * Mathf.Clamp01(savedMusic));
        }

        ApplyCategoryVolume(savedScroll, scrollSources, scrollBaseVolumes);
        ApplyCategoryVolume(savedEffects, effectsSources, effectsBaseVolumes);
        ApplyCategoryVolume(savedInteraction, interactionSources, interactionBaseVolumes);

        SetSliderValue(volumeSlider, savedMusic);
        SetSliderValue(musicSlider, savedMusic);
        SetSliderValue(scrollSlider, savedScroll);
        SetSliderValue(effectsSlider, savedEffects);
        SetSliderValue(interactionSlider, savedInteraction);
    }

    private float GetCategoryValue(List<AudioSource> sources, Dictionary<AudioSource, float> baseVolumes)
    {
        if (sources == null || sources.Count == 0) return 1f;

        for (int i = 0; i < sources.Count; i++)
        {
            AudioSource source = sources[i];
            if (source == null) continue;

            if (baseVolumes.TryGetValue(source, out float baseVolume) && baseVolume > 0f)
            {
                return Mathf.Clamp01(source.volume / baseVolume);
            }
        }

        return 1f;
    }

    private void CacheBaseVolumes(List<AudioSource> sources, Dictionary<AudioSource, float> baseVolumes)
    {
        if (sources == null) return;

        for (int i = 0; i < sources.Count; i++)
        {
            AudioSource source = sources[i];
            if (source == null) continue;
            if (!baseVolumes.ContainsKey(source))
            {
                baseVolumes[source] = source.volume;
            }
        }
    }

    private void BindSlider(Slider slider, UnityEngine.Events.UnityAction<float> handler)
    {
        if (slider == null || handler == null) return;
        slider.onValueChanged.AddListener(handler);
    }

    private void UnbindSlider(Slider slider, UnityEngine.Events.UnityAction<float> handler)
    {
        if (slider == null || handler == null) return;
        slider.onValueChanged.RemoveListener(handler);
    }

    private void SetSliderValue(Slider slider, float value)
    {
        if (slider == null) return;
        slider.SetValueWithoutNotify(Mathf.Clamp01(value));
    }

    private void SaveFloat(string key, float value)
    {
        PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
        PlayerPrefs.Save();
    }
}
