using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Build index of the main game scene")]
    public int sceneIndexToLoad = 1;

    public void PlayGame()
    {
        SceneManager.LoadScene(sceneIndexToLoad);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        // Stop play mode in the editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Quit the built application
        Application.Quit();
#endif
    }
}
