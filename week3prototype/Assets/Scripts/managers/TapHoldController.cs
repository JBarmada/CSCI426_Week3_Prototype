using UnityEngine;
using UnityEngine.UI;

public class TapHoldStart : MonoBehaviour
{
    [Header("Buttons")]
    public Button startButton;
    public Button quitButton;

    [Header("Hold Settings")]
    public float holdToQuitTime = 1.5f;

    private float holdTimer;
    private bool isHolding;
    private bool quitTriggered;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isHolding = true;
            quitTriggered = false;
            holdTimer = 0f;
        }

        if (isHolding && Input.GetKey(KeyCode.Space))
        {
            holdTimer += Time.deltaTime;

            if (holdTimer >= holdToQuitTime && !quitTriggered)
            {
                quitTriggered = true;
                quitButton.onClick.Invoke();
            }
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (!quitTriggered)
            {
                startButton.onClick.Invoke();
            }

            isHolding = false;
        }
    }
}
