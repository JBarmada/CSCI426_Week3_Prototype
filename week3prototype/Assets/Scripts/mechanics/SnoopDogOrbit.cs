using UnityEngine;

namespace Mechanics
{
    public class SnoopDogOrbit : MonoBehaviour
    {
        [Header("Orbit Settings")]
        public float radius = 80f;
        public float degreesPerSecond = 90f;

        private RectTransform _rt;
        private Vector3 _centerLocalPos;
        private float _angle;

        void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _centerLocalPos = transform.localPosition;
            _angle = Random.Range(0f, 360f);
        }

        void Update()
        {
            if (GameMenusManager.Instance != null && GameMenusManager.Instance.IsPaused) return;

            _angle += degreesPerSecond * Time.deltaTime;
            if (_angle > 360f) _angle -= 360f;

            float rad = _angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0f);

            if (_rt != null)
            {
                _rt.anchoredPosition = (Vector2)_centerLocalPos + (Vector2)offset;
            }
            else
            {
                transform.localPosition = _centerLocalPos + offset;
            }
        }
    }
}
