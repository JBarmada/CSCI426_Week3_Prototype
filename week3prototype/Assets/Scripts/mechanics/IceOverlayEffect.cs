using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Mechanics
{
    public class IceOverlayEffect : MonoBehaviour
    {
        public static IceOverlayEffect Active;
        private static int _activeCount;
        private static float _cachedInertiaSense;
        private static bool _hasCachedInertiaSense;
        [Header("Overlay")]
        public Graphic overlayGraphic;
        public float fadeInDuration = 0.3f;
        public float effectDuration = 5f;

        [Header("Gameplay")]
        public NewScrollMechanic scrollMechanic;
        public float inertiaSenseMultiplier = 10f;

        [Header("Audio")]
        public AudioClip frostLoopClip;
        public float frostVolume = 1f;

        [Header("Music")]
        [Range(0.1f, 2f)] public float musicPitchMultiplier = 0.7f;

        private CanvasGroup _canvasGroup;
        private AudioSource _audioSource;
        private bool _musicPitchAdjusted;
        private float _previousInertiaSense;
        private bool _inertiaAdjusted;
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

            _audioSource.clip = frostLoopClip;
            _audioSource.loop = true;
            _audioSource.volume = frostVolume;

            if (scrollMechanic == null)
            {
                scrollMechanic = FindFirstObjectByType<NewScrollMechanic>();
            }

            if (FireOverlayEffect.Active != null)
            {
                FireOverlayEffect.Active.ForceEnd();
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
                scrollMechanic.inertiaSense = Mathf.Min(_previousInertiaSense * inertiaSenseMultiplier, 3f);
                _inertiaAdjusted = true;
            }

            if (BackgroundMusic.Instance != null)
            {
                float targetPitch = BackgroundMusic.Instance.CurrentPitch * musicPitchMultiplier;
                BackgroundMusic.Instance.PushPitch(targetPitch);
                _musicPitchAdjusted = true;
            }

            StartCoroutine(EffectRoutine());
        }

        IEnumerator EffectRoutine()
        {
            yield return FadeCanvas(0f, 1f, fadeInDuration);

            if (frostLoopClip != null)
            {
                _audioSource.Play();
            }

            yield return new WaitForSeconds(effectDuration);

            if (_audioSource != null) _audioSource.Stop();
            yield return FadeCanvas(1f, 0f, fadeInDuration);

            RestoreMusicPitch();
            RestoreInertiaSense();

            EndAndDestroy();
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
