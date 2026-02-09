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
        public bool parentToPost;       // Should it move with the post?
        public float destroyDelay;      // Auto destroy time (0 = no destroy)
    }

    [Header("Configuration")]
    public List<TypeEffect> effectsList; 

    // Internal dictionary for faster lookup, instead of looping through list every time
    private Dictionary<PostInfo.PostType, TypeEffect> effectsDict;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Build the dictionary
        effectsDict = new Dictionary<PostInfo.PostType, TypeEffect>();
        foreach (var effect in effectsList)
        {
            if (!effectsDict.ContainsKey(effect.type))
            {
                effectsDict.Add(effect.type, effect);
            }
        }
    }

    /// <summary>
    /// Spawns the effect associated with the specific PostType.
    /// </summary>
    /// <param name="type">The type of post (Gold, Positive, etc)</param>
    /// <param name="targetTransform">Where should the effect appear?</param>
    public void TriggerEffect(PostInfo.PostType type, Transform targetTransform)
    {
        if (effectsDict.TryGetValue(type, out TypeEffect effectData))
        {
            if (effectData.effectPrefab == null) return;

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