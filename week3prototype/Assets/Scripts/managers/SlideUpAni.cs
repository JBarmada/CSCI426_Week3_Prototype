using UnityEngine;
using System.Collections;

public class SlideUpPanel : MonoBehaviour
{
    public RectTransform panel; 
    public float slideDuration = 0.4f;

    Vector2 hiddenPos;
    Vector2 visiblePos;
    Coroutine currentRoutine;

    void Awake()
    {
        visiblePos = Vector2.zero;
        hiddenPos = new Vector2(0, -Screen.height);
        panel.anchoredPosition = hiddenPos;
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        StartSlide(hiddenPos, visiblePos);
    }

    public void Hide()
    {
        if (!gameObject.activeInHierarchy)
        {
            panel.anchoredPosition = hiddenPos;
            return;
        }

        StartSlide(visiblePos, hiddenPos, () =>
        {
            gameObject.SetActive(false);
        });
    }

    void StartSlide(Vector2 from, Vector2 to, System.Action onComplete = null)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(Slide(from, to, onComplete));
    }

    IEnumerator Slide(Vector2 from, Vector2 to, System.Action onComplete)
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / slideDuration;
            panel.anchoredPosition = Vector2.Lerp(from, to, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }

        panel.anchoredPosition = to;
        onComplete?.Invoke();
    }
}
