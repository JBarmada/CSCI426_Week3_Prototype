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

        public void InitializeRandom()
        {
            // Probabilities: Gold (10%), Positive (30%), Negative (30%), Neutral (30%)
            float val = Random.value;

            if (val < 0.10f)
            {
                currentType = PostType.Gold;
            }
            else if (val < 0.40f) 
            {
                currentType = PostType.Positive;
            }
            else if (val < 0.70f) 
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
                    targetImage.color = new Color(0f, 0f, 1f, alpha / 255f);
                    break;
            }
        }
    }
}