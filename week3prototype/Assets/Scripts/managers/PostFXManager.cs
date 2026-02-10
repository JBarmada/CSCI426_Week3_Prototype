using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mechanics; // To use PostInfo.PostType

public class PostFXManager : MonoBehaviour
{
    public static PostFXManager Instance;

    [System.Serializable]
    public struct TypeEffect
    {
        public PostInfo.PostType type;
        public GameObject effectPrefab; // Confetti, Explosion, Sparkles, etc.
        public AudioClip effectSound;   // NEW: Unique sound for this effect (e.g. party horn)
        public bool parentToPost;       // Should it move with the post?
        public float destroyDelay;      // Auto destroy time (0 = no destroy)
    }

    [System.Serializable]
    public struct SpecialEffect
    {
        public PostInfo.PostSpecial special;
        public GameObject effectPrefab;
        public AudioClip effectSound;
        public bool parentToPost;
        public bool useShakeContainer; // NEW: Spawn on shake container
        public float destroyDelay;
    }

    [System.Serializable]
    public struct SpecialConversionRule
    {
        public string name; // purely for inspector organization
        public PostInfo.PostSpecial specialType;
        [Tooltip("Type of post to replace (e.g. Negative, Neutral)")]
        public PostInfo.PostType targetTypeToConvert; 
        
        public bool addOnStart;
        [Range(0f, 1f)] public float startRatio;

        public bool addOnFlip;
        [Range(0f, 1f)] public float flipRatio;
    }

    [Header("Configuration")]
    public List<TypeEffect> effectsList;
    public List<SpecialEffect> specialEffectsList;
    public List<SpecialConversionRule> conversionRules;


    [Header("References")]
    public AudioSource audioSource; // Assign this in Inspector!
    public Transform shakeContainer; // Assign or auto-detect


    // Internal dictionary for faster lookup, instead of looping through list every time
    private Dictionary<PostInfo.PostType, TypeEffect> effectsDict;
    private Dictionary<PostInfo.PostSpecial, SpecialEffect> specialEffectsDict;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Auto-add AudioSource if missing
        if (audioSource == null) 
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Auto-detect shake container if missing
        if (shakeContainer == null)
        {
            var scrollMechanic = FindObjectOfType<NewScrollMechanic>();
            if (scrollMechanic != null)
            {
                shakeContainer = scrollMechanic.targetCanvas; // Assuming targetCanvas is the shake container
            }
        }

        // Build the dictionary
        effectsDict = new Dictionary<PostInfo.PostType, TypeEffect>();
        foreach (var effect in effectsList)
        {
            // Avoid duplicates
            if (!effectsDict.ContainsKey(effect.type))
            {
                effectsDict.Add(effect.type, effect);
            }
        }

        specialEffectsDict = new Dictionary<PostInfo.PostSpecial, SpecialEffect>();
        foreach (var effect in specialEffectsList)
        {
            if (!specialEffectsDict.ContainsKey(effect.special))
            {
                specialEffectsDict.Add(effect.special, effect);
            }
        }
    }

    /// <summary>
    /// Spawns the effect AND plays the sound associated with the specific PostType.
    /// </summary>
    /// <param name="type">The type of post (Gold, Positive, etc)</param>
    /// <param name="targetTransform">Where should the effect appear?</param>
    public void TriggerEffect(PostInfo.PostType type, Transform targetTransform)
    {
        if (effectsDict.TryGetValue(type, out TypeEffect effectData))
        {
            GameObject instance = null;

            // 1. Spawn Visual Effect (if exists)
            if (effectData.effectPrefab != null)
            {
                Transform parent = effectData.parentToPost ? targetTransform : targetTransform.root;
                instance = Instantiate(effectData.effectPrefab, parent);
                
                if (!effectData.parentToPost)
                {
                    instance.transform.position = targetTransform.position;
                }

                if (effectData.destroyDelay > 0f)
                {
                    Destroy(instance, effectData.destroyDelay);
                }
            }

            // 2. Play Sound (attached to instance if possible, so it stops on destroy)
            if (effectData.effectSound != null)
            {
                if (instance != null)
                {
                    AudioSource instSource = instance.GetComponent<AudioSource>();
                    if (instSource == null) instSource = instance.AddComponent<AudioSource>();
                    
                    // Copy basic settings from manager source
                    if (audioSource != null)
                    {
                        instSource.volume = audioSource.volume;
                        instSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
                    }
                    
                    instSource.PlayOneShot(effectData.effectSound);
                }
                else if (audioSource != null)
                {
                    audioSource.PlayOneShot(effectData.effectSound);
                }
            }
        }
    }

    public void TriggerSpecialEffect(PostInfo.PostSpecial special, Transform targetTransform)
    {
        if (specialEffectsDict == null) return;
        if (special == PostInfo.PostSpecial.None) return;

        if (specialEffectsDict.TryGetValue(special, out SpecialEffect effectData))
        {
            GameObject instance = null;

            if (effectData.effectPrefab != null)
            {
                Transform parent = null;

                if (effectData.parentToPost) 
                {
                    parent = targetTransform;
                }
                else if (effectData.useShakeContainer && shakeContainer != null) 
                {
                    parent = shakeContainer;
                }
                else 
                {
                    parent = targetTransform.root;
                }

                instance = Instantiate(effectData.effectPrefab, parent);

                if (effectData.useShakeContainer)
                {
                    instance.transform.SetAsLastSibling();
                }

                if (!effectData.parentToPost && !effectData.useShakeContainer)
                {
                    instance.transform.position = targetTransform.position;
                }

                if (effectData.destroyDelay > 0f)
                {
                    Destroy(instance, effectData.destroyDelay);
                }
            }

            // Play Sound on the instance to respect its lifecycle
            if (effectData.effectSound != null)
            {
                if (instance != null)
                {
                    AudioSource instSource = instance.GetComponent<AudioSource>();
                    if (instSource == null) instSource = instance.AddComponent<AudioSource>();
                    
                    if (audioSource != null)
                    {
                        instSource.volume = audioSource.volume;
                        instSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
                    }

                    instSource.PlayOneShot(effectData.effectSound);
                }
                else if (audioSource != null)
                {
                    audioSource.PlayOneShot(effectData.effectSound);
                }
            }
        }
    }
}