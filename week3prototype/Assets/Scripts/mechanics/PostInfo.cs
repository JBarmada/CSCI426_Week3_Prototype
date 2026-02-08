using UnityEngine;
using UnityEngine.UI;

namespace Mechanics
{
    public class PostInfo : MonoBehaviour
    {
        public enum PostType { Neutral, Positive, Negative, Gold }
        public PostType currentType;

        [Header("Settings")]
        // Assigned automatically if null, but you can drag a child 'Glow' image here
        public Image targetImage;

        public void InitializeRandom()
        {
            // Probabilities: Gold (10%), Positive (30%), Negative (30%), Neutral (30%)
            float val = Random.value;

            if (val < 0.10f)
            {
                currentType = PostType.Gold;
            }
            else if (val < 0.40f) // 0.10 + 0.30
            {
                currentType = PostType.Positive;
            }
            else if (val < 0.70f) // 0.40 + 0.30
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
            // Auto-detect image if not assigned
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
                // Fallback to searching children if main object has no image
                if (targetImage == null) targetImage = GetComponentInChildren<Image>();
            }

            if (targetImage == null) return;

            switch (currentType)
            {
                case PostType.Gold:
                    targetImage.color = Color.yellow;
                    break;
                case PostType.Positive:
                    targetImage.color = Color.green;
                    break;
                case PostType.Negative:
                    targetImage.color = Color.red;
                    break;
                case PostType.Neutral:
                    targetImage.color = Color.white; 
                    break;
            }
        }
    }
}