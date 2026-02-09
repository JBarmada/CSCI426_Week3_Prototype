using UnityEngine;
using Mechanics; 
using System; // Required for Actions

public class GameStatsManager : MonoBehaviour
{
    // Singleton instance for easy access
    public static GameStatsManager Instance;
    
    // ---Event that UI can listen to ---
    public event Action OnStatsChanged; 

    [Header("General Stats")]
    public int postsScrolled = 0;
    public int totalLikes = 0;

    [Header("Like Breakdown")]
    public int likedGold = 0;
    public int likedPositive = 0;
    public int likedNegative = 0;
    public int likedNeutral = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void TrackScroll()
    {
        postsScrolled++;
        OnStatsChanged?.Invoke(); // Notify UI
    }

    public void TrackLike(PostInfo.PostType type)
    {
        totalLikes++;

        switch (type)
        {
            case PostInfo.PostType.Gold:
                likedGold++;
                break;
            case PostInfo.PostType.Positive:
                likedPositive++;
                break;
            case PostInfo.PostType.Negative:
                likedNegative++;
                break;
            case PostInfo.PostType.Neutral:
                likedNeutral++;
                break;
        }
        
        OnStatsChanged?.Invoke(); // Notify UI
        Debug.Log($"Stats Update: Total Likes: {totalLikes} | Type: {type}");
    }
}