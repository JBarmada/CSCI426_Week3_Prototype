using UnityEngine;
using UnityEngine.UI;

namespace Mechanics
{
    public class PostInfo : MonoBehaviour
    {
        public enum PostType { Neutral, Positive, Negative, Gold }
        public enum PostSpecial { None, SpaceInvader, Cat, Ice, Fire, Snoopdog }
        public PostType currentType;
        public PostSpecial currentSpecial = PostSpecial.None;

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

            currentSpecial = PostSpecial.None;

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

        public void SetSpecial(PostSpecial special)
        {
            currentSpecial = special;
        }

        public string GetDisplayLabel(int index)
        {
            if (currentSpecial != PostSpecial.None)
            {
                return $"{GetSpecialLabel(currentSpecial)} Post #{index}";
            }

            switch (currentType)
            {
                case PostType.Gold:
                    return $"Gold Post #{index}";
                case PostType.Positive:
                    return $"Positive Post #{index}";
                case PostType.Negative:
                    return $"Negative Post #{index}";
                default:
                    return $"Neutral Post #{index}";
            }
        }

        private string GetSpecialLabel(PostSpecial special)
        {
            switch (special)
            {
                case PostSpecial.SpaceInvader:
                    return "Space Invader";
                case PostSpecial.Cat:
                    return "Cat";
                case PostSpecial.Ice:
                    return "Ice";
                case PostSpecial.Fire:
                    return "Fire";
                case PostSpecial.Snoopdog:
                    return "Snoopdog";
                default:
                    return "Special";
            }
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

        public void RefreshVisuals()
        {
            UpdateVisuals();
        }
    }
}