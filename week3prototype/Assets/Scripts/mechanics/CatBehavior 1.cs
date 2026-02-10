using UnityEngine;

namespace Mechanics
{
    /// <summary>
    /// Simple behavior for the "Cat" effect.
    /// Moves the object across the screen (UI space), bouncing off edges.
    /// Respects GameMenusManager.Instance.IsPaused.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CatBehavior : MonoBehaviour
    {
        [Header("Bouncing Settings")]
        public float minSpeed = 300f;
        public float maxSpeed = 600f;

        private RectTransform _rt;
        private RectTransform _parentRt;
        private Vector2 _velocity;

        void Awake()
        {
            _rt = GetComponent<RectTransform>();
        }

        void Start()
        {
            // Center anchors to make movement logic simpler (relative to center)
            if (_rt != null)
            {
                _rt.anchorMin = new Vector2(0.5f, 0.5f);
                _rt.anchorMax = new Vector2(0.5f, 0.5f);
                _rt.anchoredPosition = Vector2.zero;
            }

            _parentRt = transform.parent as RectTransform;

            // Random direction
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float speed = Random.Range(minSpeed, maxSpeed);
            _velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;

            // Flip visual if moving left initially
            CheckFlip();
        }

        void Update()
        {
            // 1. Pause Check
            if (GameMenusManager.Instance != null && GameMenusManager.Instance.IsPaused) 
                return;

            if (_rt == null || _parentRt == null) return;

            // 2. Move
            _rt.anchoredPosition += _velocity * Time.deltaTime;

            // 3. Bounce Checks
            CheckBounds();
        }

        void CheckBounds()
        {
            // Parent Rect is relative to parent pivot.
            // If parent is full screen (CatSwarmEffect), its rect is typically centered if pivot is 0.5,0.5
            // But if anchors are 0,0-1,1 and pivot is 0.5,0.5, rect xMin is -width/2.

            Rect parentRect = _parentRt.rect;
            Vector2 pos = _rt.anchoredPosition;

            // Simple point check (center of cat)
            // Left
            if (pos.x < parentRect.xMin && _velocity.x < 0) 
            {
                ReflectX();
            }
            // Right
            if (pos.x > parentRect.xMax && _velocity.x > 0) 
            {
                ReflectX();
            }
            // Bottom
            if (pos.y < parentRect.yMin && _velocity.y < 0) 
            {
                ReflectY();
            }
            // Top
            if (pos.y > parentRect.yMax && _velocity.y > 0) 
            {
                ReflectY();
            }
        }

        void ReflectX()
        {
            _velocity.x = -_velocity.x;
            CheckFlip();
        }

        void ReflectY()
        {
            _velocity.y = -_velocity.y;
        }

        void CheckFlip()
        {
            // Assuming visual faces Right by default.
            // If vel.x < 0, scale.x = -1.
            // If vel.x > 0, scale.x = 1.
            
            if (_velocity.x != 0)
            {
                Vector3 s = transform.localScale;
                s.x = Mathf.Abs(s.x) * Mathf.Sign(_velocity.x);
                transform.localScale = s;
            }
        }
    }
}


