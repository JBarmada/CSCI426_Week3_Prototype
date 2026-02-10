using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Mechanics
{
    public class IceOverlayEffect : MonoBehaviour
    {
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

            if (scrollMechanic != null)
            {
                _previousInertiaSense = scrollMechanic.inertiaSense;
                scrollMechanic.inertiaSense = _previousInertiaSense * inertiaSenseMultiplier;
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

            _audioSource.Stop();
            yield return FadeCanvas(1f, 0f, fadeInDuration);

            RestoreMusicPitch();
            RestoreInertiaSense();

            Destroy(gameObject);
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
            RestoreMusicPitch();
            RestoreInertiaSense();
        }

        void RestoreInertiaSense()
        {
            if (scrollMechanic != null && _inertiaAdjusted)
            {
                scrollMechanic.inertiaSense = _previousInertiaSense;
                _inertiaAdjusted = false;
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
