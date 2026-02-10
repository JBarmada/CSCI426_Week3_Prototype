using UnityEngine;
using UnityEngine.UI;

public class MenuSpaceInput : MonoBehaviour
{
    [Header("Menu Root")]
    public GameObject menuRoot;

    [Header("Buttons")]
    public Button primaryButton;
    public Button exitButton;

    [Header("Hold Settings")]
    public float holdToExitTime = 1.5f;

    private float holdTimer;
    private bool isHolding;
    private bool exitTriggered;

    void Awake()
    {
        if (menuRoot == null)
        {
            menuRoot = gameObject;
        }
    }

    void Update()
    {
        if (menuRoot == null || !menuRoot.activeInHierarchy) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            isHolding = true;
            exitTriggered = false;
            holdTimer = 0f;
        }

        if (isHolding && Input.GetKey(KeyCode.Space))
        {
            holdTimer += Time.unscaledDeltaTime;

            if (holdTimer >= holdToExitTime && !exitTriggered)
            {
                exitTriggered = true;
                if (exitButton != null)
                {
                    exitButton.onClick.Invoke();
                }
            }
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (!exitTriggered && primaryButton != null)
            {
                primaryButton.onClick.Invoke();
            }

            isHolding = false;
        }
    }
}
