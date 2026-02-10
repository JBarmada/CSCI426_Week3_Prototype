using System.Collections;
using UnityEngine;

namespace Mechanics
{
    public class CatSwarmEffect : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Prefab for the individual bouncing cat")]
        public GameObject catVisualPrefab;
        public int spawnCount = 5;
        public float effectDuration = 5.0f;
        public float musicFadeTime = 0.5f;
        public AudioClip nyanMusicClip;

        private BackgroundMusic _music;
        private bool _musicPushed;

        void Start()
        {
            // 1. Setup Container (Screen Fill)
            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;
            }

            // 2. Music Control
            _music = BackgroundMusic.Instance;
            if (_music != null)
            {
                if (nyanMusicClip != null)
                {
                    _music.PushTemporaryMusic(nyanMusicClip, musicFadeTime);
                    _musicPushed = true;
                }
                else
                {
                    _music.FadeOut(musicFadeTime);
                }
            }

            // 3. Spawn Cats
            SpawnCats();

            // 4. Timer to die
            StartCoroutine(LifeCycleRoutine());
        }

        void SpawnCats()
        {
            if (catVisualPrefab == null)
            {
                Debug.LogWarning("CatSwarmEffect: No Visual Prefab assigned!");
                return;
            }

            for (int i = 0; i < spawnCount; i++)
            {
                GameObject cat = Instantiate(catVisualPrefab, transform);
                // Reset local pos to 0, or let them spawn at center and explode out
                cat.transform.localPosition = Vector3.zero;
                cat.transform.localScale = Vector3.one; 
            }
        }

        IEnumerator LifeCycleRoutine()
        {
            yield return new WaitForSeconds(effectDuration);

            // Restore Music
            if (_music != null)
            {
                if (nyanMusicClip != null)
                {
                    if (_musicPushed)
                    {
                        _music.PopTemporaryMusic(nyanMusicClip, musicFadeTime);
                        _musicPushed = false;
                    }
                }
                else
                {
                    _music.FadeIn(musicFadeTime);
                }
            }

            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (_music != null && nyanMusicClip != null && _musicPushed)
            {
                _music.PopTemporaryMusic(nyanMusicClip, musicFadeTime);
                _musicPushed = false;
            }
        }
    }
}
