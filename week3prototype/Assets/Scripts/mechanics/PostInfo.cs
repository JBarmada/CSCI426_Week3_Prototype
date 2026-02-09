using UnityEngine;
using UnityEngine.UI;

namespace Mechanics
{
    public class PostInfo : MonoBehaviour
    {
        public enum PostType { Neutral, Positive, Negative, Gold }
        public PostType currentType;

        [Header("Settings")]
        // CHANGED: Now an array to target all 4 images at once
        public RawImage[] targetImages; 

        [Range(0, 255)] public int alpha = 56;
        [Range(0, 255)] public int blueAlpha = 100;
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
            // Auto-detect ALL RawImages if the list is empty
            if (targetImages == null || targetImages.Length == 0)
            {
                targetImages = GetComponentsInChildren<RawImage>();
            }

            if (targetImages == null || targetImages.Length == 0) 
            {
                Debug.LogWarning($"No RawImages found on {gameObject.name} or its children!");
                return;
            }

            Color colorToApply = Color.white;

            // Determine color based on type
            switch (currentType)
            {
                case PostType.Gold:
                    colorToApply = new Color(1f, 0.84f, 0f, alpha / 255f);
                    break;
                case PostType.Positive:
                    colorToApply = new Color(0f, 1f, 0f, alpha / 255f);
                    break;
                case PostType.Negative:
                    colorToApply = new Color(1f, 0f, 0f, alpha / 255f);
                    break;
                case PostType.Neutral:
                    colorToApply = new Color(0f, 0f, 1f, blueAlpha / 255f);
                    break;
            }

            // Apply color to ALL images
            foreach (RawImage img in targetImages)
            {
                if (img != null)
                {
                    img.color = colorToApply;
                }
            }
        }
    }
}