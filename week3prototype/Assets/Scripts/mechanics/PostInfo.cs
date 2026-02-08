using UnityEngine;
using UnityEngine.UI;

namespace Mechanics
{
    public class PostInfo : MonoBehaviour
    {
        public enum PostType { Neutral, Positive, Negative, Gold }
        public PostType currentType;

        [Header("Settings")]
        // CHANGED: Now looking for a RawImage instead of Image
        public RawImage targetImage; 
        [Range(0, 255)] public int alpha = 56;
        [Range(0,255)] public int blueAlpha = 100;
        [Range(0f, 1f)] public float goldProbability = 0.1f;
        [Range(0f, 1f)] public float positiveProbability = 0.3f;
        [Range(0f, 1f)] public float negativeProbability = 0.1f;


        public void InitializeRandom()
        {
            // Probabilities: Gold (10%), Positive (30%), Negative (10%), Neutral (50%)
            float val = Random.value;

            if (val < goldProbability)
            {
                currentType = PostType.Gold;
            }
            else if (val < goldProbability + positiveProbability) 
            {
                currentType = PostType.Positive;
            }
            else if (val < goldProbability + positiveProbability + negativeProbability) 
            {
                currentType = PostType.Negative;
            }
            else
            {
                currentType = PostType.Neutral;
            }

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            // Auto-detect RawImage if not assigned
            if (targetImage == null)
            {
                // First, look on this object
                targetImage = GetComponent<RawImage>();
                
                // If not found, look in the children (This is likely where yours is)
                if (targetImage == null) targetImage = GetComponentInChildren<RawImage>();
            }

            if (targetImage == null) 
            {
                Debug.LogWarning($"No RawImage found on {gameObject.name} or its children!");
                return;
            }

            // Apply colors based on type
            switch (currentType)
            {
                case PostType.Gold:
                    targetImage.color = new Color(1f, 0.84f, 0f, alpha / 255f);
                    break;
                case PostType.Positive:
                    targetImage.color = new Color(0f, 1f, 0f, alpha / 255f);
                    break;
                case PostType.Negative:
                    targetImage.color = new Color(1f, 0f, 0f, alpha / 255f);
                    break;
                case PostType.Neutral:
                    targetImage.color = new Color(0f, 0f, 1f, blueAlpha / 255f);
                    break;
            }
        }
    }
}