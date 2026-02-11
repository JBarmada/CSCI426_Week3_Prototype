using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mechanics
{
    public class FireOverlayEffect : MonoBehaviour
    {
        public static FireOverlayEffect Active;
        private static int _activeCount;
        private static float _cachedInertiaSense;
        private static bool _hasCachedInertiaSense;
        [Header("Overlay")]
        public Graphic overlayGraphic;
        public float fadeInDuration = 0.3f;
        public float effectDuration = 5f;

        [Header("Gameplay")]
        public NewScrollMechanic scrollMechanic;
        public float inertiaSenseMultiplier = 0.7f;

        [Header("Post Changes")]
        [Range(0f, 1f)] public float changeFraction = 0.6f;
        [Range(0f, 1f)] public float negativeChance = 0.5f;

        [Header("Audio")]
        public AudioClip fireLoopClip;
        public float fireVolume = 1f;

        [Header("Music")]
        [Range(0.1f, 2f)] public float musicPitchMultiplier = 1f;

        private CanvasGroup _canvasGroup;
        private AudioSource _audioSource;
        private bool _musicPitchAdjusted;
        private float _previousInertiaSense;
        private bool _inertiaAdjusted;
        private Dictionary<PostInfo, PostInfo.PostType> _originalTypes;
        private bool _isEnding;
        private bool _registeredInertia;

        void Start()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (overlayGraphic == null)
            {
                overlayGraphic = GetComponentInChildren<Graphic>();
            }

            _canvasGroup.alpha = 0f;

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();

            _audioSource.clip = fireLoopClip;
            _audioSource.loop = true;
            _audioSource.volume = fireVolume;

            if (scrollMechanic == null)
            {
                scrollMechanic = FindFirstObjectByType<NewScrollMechanic>();
            }

            if (IceOverlayEffect.Active != null)
            {
                IceOverlayEffect.Active.ForceEnd();
            }

            Active = this;

            if (scrollMechanic != null)
            {
                if (!_hasCachedInertiaSense)
                {
                    _cachedInertiaSense = scrollMechanic.inertiaSense;
                    _hasCachedInertiaSense = true;
                }

                _previousInertiaSense = _cachedInertiaSense;
                _activeCount++;
                _registeredInertia = true;
                scrollMechanic.inertiaSense = Mathf.Max(_previousInertiaSense * inertiaSenseMultiplier, 0.25f);
                _inertiaAdjusted = true;
            }

            if (BackgroundMusic.Instance != null)
            {
                float targetPitch = BackgroundMusic.Instance.CurrentPitch * musicPitchMultiplier;
                BackgroundMusic.Instance.PushPitch(targetPitch);
                _musicPitchAdjusted = true;
            }

            ApplyPostChanges();

            StartCoroutine(EffectRoutine());
        }

        IEnumerator EffectRoutine()
        {
            yield return FadeCanvas(0f, 1f, fadeInDuration);

            if (fireLoopClip != null)
            {
                _audioSource.Play();
            }

            yield return new WaitForSeconds(effectDuration);

            if (_audioSource != null) _audioSource.Stop();
            yield return FadeCanvas(1f, 0f, fadeInDuration);

            RestorePosts();
            RestoreMusicPitch();
            RestoreInertiaSense();

            EndAndDestroy();
        }

        void ApplyPostChanges()
        {
            PostInfo[] allPosts = FindObjectsByType<PostInfo>(FindObjectsSortMode.None);
            if (allPosts == null || allPosts.Length == 0) return;

            List<PostInfo> candidates = new List<PostInfo>();
            foreach (PostInfo post in allPosts)
            {
                if (post == null) continue;
                if (post.currentSpecial != PostInfo.PostSpecial.None) continue;
                candidates.Add(post);
            }

            if (candidates.Count == 0) return;

            int toChange = Mathf.Clamp(Mathf.RoundToInt(candidates.Count * changeFraction), 0, candidates.Count);
            if (toChange == 0) return;

            _originalTypes = new Dictionary<PostInfo, PostInfo.PostType>(toChange);

            for (int i = 0; i < candidates.Count; i++)
            {
                int swapIndex = Random.Range(i, candidates.Count);
                PostInfo temp = candidates[i];
                candidates[i] = candidates[swapIndex];
                candidates[swapIndex] = temp;
            }

            for (int i = 0; i < toChange; i++)
            {
                PostInfo post = candidates[i];
                _originalTypes[post] = post.currentType;

                post.currentType = Random.value < negativeChance ? PostInfo.PostType.Negative : PostInfo.PostType.Neutral;
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
                post.RefreshVisuals();
                UpdatePostLabel(post);
            }

            _originalTypes.Clear();
            _originalTypes = null;
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

        IEnumerator FadeCanvas(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                _canvasGroup.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            _canvasGroup.alpha = from;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            _canvasGroup.alpha = to;
        }

        void OnDestroy()
        {
            if (Active == this) Active = null;
            RestorePosts();
            RestoreMusicPitch();
            RestoreInertiaSense();
        }

        public void ForceEnd()
        {
            if (_isEnding) return;
            _isEnding = true;

            StopAllCoroutines();

            if (_audioSource != null) _audioSource.Stop();
            _canvasGroup.alpha = 0f;

            RestorePosts();
            RestoreMusicPitch();
            RestoreInertiaSense();

            EndAndDestroy();
        }

        void EndAndDestroy()
        {
            if (_isEnding)
            {
                Destroy(gameObject);
                return;
            }

            _isEnding = true;
            Destroy(gameObject);
        }

        void RestoreInertiaSense()
        {
            if (scrollMechanic != null && _inertiaAdjusted)
            {
                _inertiaAdjusted = false;
                if (_registeredInertia)
                {
                    _registeredInertia = false;
                    _activeCount = Mathf.Max(0, _activeCount - 1);
                    if (_activeCount == 0 && _hasCachedInertiaSense)
                    {
                        scrollMechanic.inertiaSense = _cachedInertiaSense;
                        _hasCachedInertiaSense = false;
                    }
                }
            }
        }

        void RestoreMusicPitch()
        {
            if (BackgroundMusic.Instance != null && _musicPitchAdjusted)
            {
                BackgroundMusic.Instance.PopPitch();
                _musicPitchAdjusted = false;
            }
        }
    }
}
