using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Mechanics
{
    public class GoldRushEffect : MonoBehaviour
    {
        public static GoldRushEffect Active;

        [Header("Settings")]
        public float duration = 5f;
        public AudioClip goldRushMusicClip;
        public float musicFadeTime = 0.5f;

        private Dictionary<PostInfo, PostInfo.PostType> _originalTypes;
        private Dictionary<PostInfo, PostInfo.PostSpecial> _originalSpecials;
        private bool _musicPushed;

        public static void Trigger(GoldRushEffect prefab, float duration)
        {
            if (Active != null) return;

            GoldRushEffect effect;
            if (prefab != null)
            {
                effect = Instantiate(prefab);
            }
            else
            {
                GameObject obj = new GameObject("GoldRushEffect");
                effect = obj.AddComponent<GoldRushEffect>();
            }

            if (duration > 0f)
            {
                effect.duration = duration;
            }
        }

        void Start()
        {
            if (Active != null && Active != this)
            {
                Destroy(gameObject);
                return;
            }

            Active = this;
            if (BackgroundMusic.Instance != null && goldRushMusicClip != null)
            {
                _musicPushed = BackgroundMusic.Instance.PushTemporaryMusic(goldRushMusicClip, musicFadeTime);
            }
            ApplyGold();
            StartCoroutine(LifeCycleRoutine());
        }

        IEnumerator LifeCycleRoutine()
        {
            yield return new WaitForSeconds(duration);
            RestorePosts();
            Destroy(gameObject);
        }

        void ApplyGold()
        {
            PostInfo[] allPosts = FindObjectsByType<PostInfo>(FindObjectsSortMode.None);
            if (allPosts == null || allPosts.Length == 0) return;

            _originalTypes = new Dictionary<PostInfo, PostInfo.PostType>(allPosts.Length);
            _originalSpecials = new Dictionary<PostInfo, PostInfo.PostSpecial>(allPosts.Length);
            foreach (PostInfo post in allPosts)
            {
                if (post == null) continue;
                _originalTypes[post] = post.currentType;
                _originalSpecials[post] = post.currentSpecial;
                post.currentType = PostInfo.PostType.Gold;
                post.currentSpecial = PostInfo.PostSpecial.None;
                post.RefreshVisuals();
                UpdatePostLabel(post);
            }
        }

        void RestorePosts()
        {
            if (_originalTypes == null) return;

            foreach (var pair in _originalTypes)
            {
                PostInfo post = pair.Key;
                if (post == null) continue;

                post.currentType = pair.Value;
                if (_originalSpecials != null && _originalSpecials.TryGetValue(post, out PostInfo.PostSpecial special))
                {
                    post.currentSpecial = special;
                }
                post.RefreshVisuals();
                UpdatePostLabel(post);
            }

            _originalTypes.Clear();
            _originalTypes = null;
            if (_originalSpecials != null)
            {
                _originalSpecials.Clear();
                _originalSpecials = null;
            }
        }

        void UpdatePostLabel(PostInfo post)
        {
            if (post == null) return;

            TextMeshProUGUI text = post.GetComponentInChildren<TextMeshProUGUI>();
            if (text == null) return;

            if (int.TryParse(post.gameObject.name, out int index))
            {
                text.text = post.GetDisplayLabel(index);
            }
        }

        void OnDestroy()
        {
            if (Active == this) Active = null;
            if (BackgroundMusic.Instance != null && goldRushMusicClip != null && _musicPushed)
            {
                BackgroundMusic.Instance.PopTemporaryMusic(goldRushMusicClip, musicFadeTime);
                _musicPushed = false;
            }
            RestorePosts();
        }
    }
}
