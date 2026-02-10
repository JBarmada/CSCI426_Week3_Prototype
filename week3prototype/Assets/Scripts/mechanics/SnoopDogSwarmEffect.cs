using System.Collections;
using UnityEngine;

namespace Mechanics
{
    public class SnoopDogSwarmEffect : MonoBehaviour
    {
        [Header("Settings")]
        public GameObject snoopPrefab;
        public int spawnCount = 4;
        public float effectDuration = 5f;
        public float musicFadeTime = 0.5f;
        public AudioClip snoopMusicClip;

        [Header("Spawn Tuning")]
        public float spawnRadius = 120f;
        public float orbitRadius = 80f;
        public float orbitSpeed = 90f;

        private BackgroundMusic _music;
        private bool _musicPushed;

        void Start()
        {
            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;
            }

            SpawnSnoops();

            _music = BackgroundMusic.Instance;
            if (_music != null && snoopMusicClip != null)
            {
                _music.PushTemporaryMusic(snoopMusicClip, musicFadeTime);
                _musicPushed = true;
            }

            if (effectDuration > 0f)
            {
                StartCoroutine(LifeCycleRoutine());
            }
        }

        void SpawnSnoops()
        {
            if (snoopPrefab == null)
            {
                Debug.LogWarning("SnoopDogSwarmEffect: No Snoop prefab assigned!");
                return;
            }

            for (int i = 0; i < spawnCount; i++)
            {
                GameObject snoop = Instantiate(snoopPrefab, transform);

                Vector2 startOffset = Random.insideUnitCircle * spawnRadius;

                RectTransform snoopRt = snoop.GetComponent<RectTransform>();
                if (snoopRt != null)
                {
                    snoopRt.anchoredPosition = startOffset;
                }
                else
                {
                    snoop.transform.localPosition = new Vector3(startOffset.x, startOffset.y, 0f);
                }

                SnoopDogOrbit orbit = snoop.GetComponent<SnoopDogOrbit>();
                if (orbit == null) orbit = snoop.AddComponent<SnoopDogOrbit>();
                orbit.radius = orbitRadius;
                orbit.degreesPerSecond = orbitSpeed;
            }
        }

        IEnumerator LifeCycleRoutine()
        {
            yield return new WaitForSeconds(effectDuration);
            RestoreMusic();
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            RestoreMusic();
        }

        void RestoreMusic()
        {
            if (_music != null && snoopMusicClip != null && _musicPushed)
            {
                _music.PopTemporaryMusic(snoopMusicClip, musicFadeTime);
                _musicPushed = false;
            }
        }
    }
}
