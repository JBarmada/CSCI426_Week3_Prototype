using TMPro;
using UnityEngine;

public class GameWinTimer : MonoBehaviour
{
    [Header("Timer")]
    public float winTimeSeconds = 90f;

    [Header("Dependencies")]
    public DopamineManager dopamineManager;
    public SlideUpPanel winMenu;

    [Header("UI")]
    public TextMeshProUGUI timerText;

    private float elapsed;
    private bool winTriggered;

    void Start()
    {
        elapsed = 0f;
        UpdateTimerText();
    }

    void Update()
    {
        if (winTriggered) return;
        if (GameMenusManager.Instance != null && GameMenusManager.Instance.IsPaused) return;
        if (dopamineManager == null) return;

        elapsed += Time.deltaTime;
        UpdateTimerText();

        if (elapsed < winTimeSeconds) return;

        if (dopamineManager.getCurrDop() > 0f)
        {
            winTriggered = true;

            if (GameMenusManager.Instance != null)
            {
                GameMenusManager.Instance.PauseForWin();
                GameMenusManager.Instance.PlayWinMusic();
            }

            if (winMenu != null)
            {
                winMenu.Show();
            }
        }
    }

    private void UpdateTimerText()
    {
        if (timerText == null) return;

        float remaining = Mathf.Max(0f, winTimeSeconds - elapsed);
        int minutes = Mathf.FloorToInt(remaining / 60);
        int seconds = Mathf.CeilToInt(remaining % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    } 
}
