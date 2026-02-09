using UnityEngine;

public class BulletAutoCleanup : MonoBehaviour
{
    [Tooltip("Seconds before a bullet is auto-destroyed.")]
    public float lifetimeSeconds = 10f;

    void OnEnable()
    {
        if (lifetimeSeconds <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        StartCoroutine(LifetimeCountdown());
    }

    System.Collections.IEnumerator LifetimeCountdown()
    {
        float elapsed = 0f;
        while (elapsed < lifetimeSeconds)
        {
            if (GameMenusManager.Instance != null && GameMenusManager.Instance.IsPaused)
            {
                yield return null;
                continue;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}
