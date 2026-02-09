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
        public float destroyDelay;
    }

    [Header("Configuration")]
    public List<TypeEffect> effectsList;
    public List<SpecialEffect> specialEffectsList;

    [Header("References")]
    public AudioSource audioSource; // Assign this in Inspector!

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
            // 1. Play Sound (if exists)
            if (effectData.effectSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(effectData.effectSound);
            }

            // 2. Spawn Visual Effect (if exists)
            if (effectData.effectPrefab != null)
            {
                // Determine parent
                Transform parent = effectData.parentToPost ? targetTransform : targetTransform.root; // root is usually Canvas
                
                // Spawn
                GameObject instance = Instantiate(effectData.effectPrefab, parent);
                
                // If not parented to post, we must manually set position to match target
                if (!effectData.parentToPost)
                {
                    instance.transform.position = targetTransform.position;
                }

                // Auto Destroy?
                if (effectData.destroyDelay > 0f)
                {
                    Destroy(instance, effectData.destroyDelay);
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
            if (effectData.effectSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(effectData.effectSound);
            }

            if (effectData.effectPrefab != null)
            {
                Transform parent = effectData.parentToPost ? targetTransform : targetTransform.root;
                GameObject instance = Instantiate(effectData.effectPrefab, parent);

                if (!effectData.parentToPost)
                {
                    instance.transform.position = targetTransform.position;
                }

                if (effectData.destroyDelay > 0f)
                {
                    Destroy(instance, effectData.destroyDelay);
                }
            }
        }
    }
}