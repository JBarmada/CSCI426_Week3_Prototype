using System.Collections;
using UnityEngine;

public class DopamineTracker : MonoBehaviour
{
    public DopamineManager manager;
    private float currDop;
    private bool gameNotOverBefore = false;

    public SlideUpPanel gameOverMenu; 

    [Header("Game Over Hit Stop")]
    public RectTransform dopamineBarTransform;
    public float hitStopDuration = 0.5f;
    public float barShakeIntensity = 8f;

    [Header("Game Over Audio")]
    public AudioSource gameOverSfxSource;
    public AudioClip gameOverSfxClip;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //keep track of the current dopamine value every half second or something 
        currDop = manager.getCurrDop();
        if (currDop <= 0 && !gameNotOverBefore)
        {
            gameNotOverBefore = true;
            StartCoroutine(GameOverSequence());
        }

    }

    private IEnumerator GameOverSequence()
    {
        Debug.Log("GAME OVER");

        if (GameMenusManager.Instance != null)
        {
            GameMenusManager.Instance.PauseForGameOver();
        }
        if (gameOverMenu != null)
        {
            gameOverMenu.Show();
        }

        AudioListener.pause = true;
        if (dopamineBarTransform != null)
        {
            yield return StartCoroutine(ShakeBar(hitStopDuration));
        }
        else
        {
            yield return new WaitForSecondsRealtime(hitStopDuration);
        }
        AudioListener.pause = false;

        float sfxDelay = 0f;
        if (gameOverSfxSource != null && gameOverSfxClip != null)
        {
            gameOverSfxSource.PlayOneShot(gameOverSfxClip);
            sfxDelay = gameOverSfxClip.length;
        }

        if (sfxDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(sfxDelay);
        }

        if (GameMenusManager.Instance != null)
        {
            GameMenusManager.Instance.PlayGameOverMusic();
        }
    }

    private IEnumerator ShakeBar(float duration)
    {
        Vector3 originalPos = dopamineBarTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            Vector2 offset = Random.insideUnitCircle * barShakeIntensity;
            dopamineBarTransform.localPosition = originalPos + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        dopamineBarTransform.localPosition = originalPos;
    }
}
