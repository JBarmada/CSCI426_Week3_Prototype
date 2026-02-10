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

        [Header("Audio")]
        public AudioClip frostLoopClip;
        public float frostVolume = 1f;

        [Header("Music")]
        public float musicFadeTime = 0.5f;

        private CanvasGroup _canvasGroup;
        private AudioSource _audioSource;

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

            if (BackgroundMusic.Instance != null)
            {
                BackgroundMusic.Instance.FadeOut(musicFadeTime);
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

            if (BackgroundMusic.Instance != null)
            {
                BackgroundMusic.Instance.FadeIn(musicFadeTime);
            }

            _audioSource.Stop();
            yield return FadeCanvas(1f, 0f, fadeInDuration);

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
    }
}
