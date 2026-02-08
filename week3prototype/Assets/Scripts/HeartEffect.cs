using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HeartEffect : MonoBehaviour
{
    [Header("Animation Settings")]
    public float targetScale = 17f;
    public float growTime = 0.2f;
    public float stayTime = 0.1f;
    public float shrinkTime = 0.2f;

    [Header("Position Settings")]
    public Vector2 spawnOffset = new Vector2(0f, 0f);

    private void Start()
    {
        SetupComponents();
        StartCoroutine(Animate());
    }

    private void SetupComponents()
    {
        // 1. Ignore Parent Layout
        LayoutElement layout = GetComponent<LayoutElement>();
        if (layout == null) layout = gameObject.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;

        // 2. Set Position & Rotation
        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            // Reset Z to 0 so it's visible
            rect.localPosition = new Vector3(rect.localPosition.x, rect.localPosition.y, 0f);
            rect.anchoredPosition = spawnOffset;
            rect.localRotation = Quaternion.identity;
        }

        // 3. Ensure visibility order
        transform.SetAsLastSibling();
        
        // 4. Start invisible
        transform.localScale = Vector3.zero;
    }

    private IEnumerator Animate()
    {
        // Grow Phase
        float elapsed = 0f;
        while (elapsed < growTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / growTime;
            // Smooth "Pop" ease out
            t = t * t * (3f - 2f * t); 
            transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * targetScale, t);
            yield return null;
        }

        // Stay Phase
        transform.localScale = Vector3.one * targetScale;
        yield return new WaitForSeconds(stayTime);

        // Shrink Phase
        elapsed = 0f;
        while (elapsed < shrinkTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shrinkTime;
            transform.localScale = Vector3.Lerp(Vector3.one * targetScale, Vector3.zero, t);
            yield return null;
        }

        Destroy(gameObject);
    }
}