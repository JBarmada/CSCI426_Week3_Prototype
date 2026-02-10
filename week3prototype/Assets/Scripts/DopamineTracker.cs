using UnityEngine;

public class DopamineTracker : MonoBehaviour
{
    public DopamineManager manager;
    private float currDop;
    private bool gameNotOverBefore = false;

    public SlideUpPanel gameOverMenu; 

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
            //throw up end screen 
            Debug.Log("GAME OVER");
            if (GameMenusManager.Instance != null)
            {
                GameMenusManager.Instance.PauseForGameOver();
            }
            gameOverMenu.Show(); 

            if (GameMenusManager.Instance != null)
            {
                GameMenusManager.Instance.PlayGameOverMusic();
            }

            gameNotOverBefore = true; 
        }

    }
}
