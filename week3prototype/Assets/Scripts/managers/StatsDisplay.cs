using UnityEngine;
using TMPro; // Make sure you have TextMeshPro installed
using System.Text; // For StringBuilder (optional, but cleaner)

public class StatsDisplay : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI postsScrolledText;
    public TextMeshProUGUI totalLikesText;
    
    [Header("Breakdown Texts")]
    // You can assign individual texts, or one big detailed text
    public TextMeshProUGUI breakdownText; 

    private void Start()
    {
        // Subscribe to updates
        if (GameStatsManager.Instance != null)
        {
            GameStatsManager.Instance.OnStatsChanged += UpdateUI;
            // Run once at start to initialize text
            UpdateUI();
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent errors when scene changes
        if (GameStatsManager.Instance != null)
        {
            GameStatsManager.Instance.OnStatsChanged -= UpdateUI;
        }
    }

    private void UpdateUI()
    {
        if (GameStatsManager.Instance == null) return;
        var stats = GameStatsManager.Instance;

        // 1. Update Simple Counts
        if (postsScrolledText != null) 
            postsScrolledText.text = $"Scrolled: {stats.postsScrolled}";
        
        if (totalLikesText != null) 
            totalLikesText.text = $"Likes: {stats.totalLikes}";

        // 2. Update Breakdown (e.g. "Gold: 5 | Pos: 12 ...")
        if (breakdownText != null)
        {
            breakdownText.text = 
                $"<color=yellow>Gold: {stats.likedGold}</color>\n" +
                $"<color=green>Pos: {stats.likedPositive}</color>\n" +
                $"<color=white>Neu: {stats.likedNeutral}</color>\n" +
                $"<color=red>Neg: {stats.likedNegative}</color>";
        }
    }
}