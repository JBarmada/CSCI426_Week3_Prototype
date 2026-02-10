using Mechanics;
using UnityEngine;
using UnityEngine.UI;

public class NyanCatIndicator : MonoBehaviour
{
    [Header("Dependencies")]
    public DopamineManager dopamineManager;

    [Header("Slots")]
    public Image[] catSlots;
    public int maxSlots = 3;

    [Header("Appearance")]
    public Sprite catSprite;
    public Color ghostColor = new Color(0.25f, 0.25f, 0.25f, 1f);
    public Color activeColor = Color.white;

    void Start()
    {
        if (dopamineManager == null)
        {
            dopamineManager = DopamineManager.Instance;
        }

        InitializeSlots();
        UpdateSlots(0);
    }

    void Update()
    {
        if (dopamineManager == null) return;
        int activeCount = dopamineManager.GetActiveSpecialCount(PostInfo.PostSpecial.Cat);
        UpdateSlots(activeCount);
    }

    private void InitializeSlots()
    {
        if (catSlots == null) return;

        for (int i = 0; i < catSlots.Length; i++)
        {
            if (catSlots[i] == null) continue;
            if (catSprite != null)
            {
                catSlots[i].sprite = catSprite;
            }
        }
    }

    private void UpdateSlots(int activeCount)
    {
        if (catSlots == null) return;

        int clamped = Mathf.Clamp(activeCount, 0, maxSlots);
        int slotCount = Mathf.Min(catSlots.Length, maxSlots);

        for (int i = 0; i < slotCount; i++)
        {
            if (catSlots[i] == null) continue;
            catSlots[i].color = i < clamped ? activeColor : ghostColor;
        }
    }
}
