using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameMenusManager : MonoBehaviour
{
    public static GameMenusManager Instance { get; private set; }

    [Header("UI")]
    public SlideUpPanel pauseMenu;

    [Header("Game Over Music")]
    public AudioClip gameOverMusicClip;
    public float gameOverMusicFadeDuration = 0.5f;

    [Header("State")]
    public bool IsPaused { get; private set; }

    private bool tabHeld;
    private bool gameOverMusicActive;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        Time.timeScale = 1f;
        IsPaused = false;

        if (pauseMenu)
            pauseMenu.gameObject.SetActive(false);
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Restart
        if (keyboard.rKey.wasPressedThisFrame)
        {
            RestartScene();
        }

        // Pause toggle (Escape)
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }

        // Pause toggle (Tab, prevent hold repeat)
        if (keyboard.tabKey.isPressed && !tabHeld)
        {
            TogglePause();
        }

        tabHeld = keyboard.tabKey.isPressed;
    }

    // ======================
    // PAUSE CONTROL
    // ======================

    public void TogglePause()
    {
        IsPaused = !IsPaused;
        Time.timeScale = IsPaused ? 0f : 1f;

        if (!pauseMenu) return;

        if (IsPaused)
            pauseMenu.Show();
        else
            pauseMenu.Hide();
    }

    public void ResumeGame()
    {
        if (!IsPaused) return;

        IsPaused = false;
        Time.timeScale = 1f;

        if (pauseMenu)
            pauseMenu.Hide();
    }

    public void PauseForGameOver()
    {
        IsPaused = true;
        Time.timeScale = 0f;

        if (pauseMenu)
            pauseMenu.Hide();
    }

    public void PlayGameOverMusic()
    {
        if (!gameOverMusicActive && BackgroundMusic.Instance != null && gameOverMusicClip != null)
        {
            float duration = Time.timeScale == 0f ? 0f : gameOverMusicFadeDuration;
            gameOverMusicActive = BackgroundMusic.Instance.PushTemporaryMusic(gameOverMusicClip, duration);
        }
    }

    public void StopGameOverMusic()
    {
        if (gameOverMusicActive && BackgroundMusic.Instance != null && gameOverMusicClip != null)
        {
            BackgroundMusic.Instance.PopTemporaryMusic(gameOverMusicClip, gameOverMusicFadeDuration);
            gameOverMusicActive = false;
        }
    }

    public void RestartScene()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        if (BackgroundMusic.Instance != null)
        {
            StopGameOverMusic();
            BackgroundMusic.Instance.RestartMusic();
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
