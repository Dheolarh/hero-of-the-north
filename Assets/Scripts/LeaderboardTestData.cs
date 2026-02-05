using UnityEngine;

/// <summary>
/// Test script to populate leaderboard with mock data
/// Attach to a button or call from inspector for testing
/// </summary>
public class LeaderboardTestData : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool includeAvatars = false;
    [SerializeField] private int numberOfPlayers = 50;

    /// <summary>
    /// Call this method to test the leaderboard with mock data
    /// </summary>
    public void PopulateTestLeaderboard()
    {
        if (LeaderboardUI.Instance == null)
        {
            Debug.LogError("[LeaderboardTestData] LeaderboardUI.Instance is null!");
            return;
        }

        // Create mock leaderboard entries
        DevvitBridge.LeaderboardEntry[] mockEntries = GenerateMockEntries(numberOfPlayers);
        
        // Display on leaderboard
        LeaderboardUI.Instance.DisplayLeaderboard(mockEntries);
        
        // Create mock player standing
        DevvitBridge.PlayerStanding mockStanding = new DevvitBridge.PlayerStanding
        {
            rank = 42,
            totalPoints = 850,
            levelsCompleted = 5
        };
        
        LeaderboardUI.Instance.UpdatePlayerStanding(mockStanding);
        
        Debug.Log($"[LeaderboardTestData] Populated leaderboard with {mockEntries.Length} entries");
    }

    private DevvitBridge.LeaderboardEntry[] GenerateMockEntries(int count)
    {
        string[] heroNames = new string[]
        {
            "FrostBlade", "IceWarrior", "SnowKnight", "ArcticHero", "GlacierGuard",
            "WinterChampion", "FrozenLegend", "ColdSteel", "NorthStar", "BlizzardKing",
            "IceShard", "FrostFury", "SnowStorm", "ArcticAce", "GlacialForce",
            "WinterWolf", "FrozenPhoenix", "IceDragon", "NorthWind", "PolarPower",
            "FrostFang", "IceBound", "SnowShadow", "ArcticAvenger", "GlacierGhost",
            "WinterWarlock", "FrozenFist", "ColdHeart", "NorthernLight", "BlizzardBeast",
            "IceBreaker", "FrostFlame", "SnowSentinel", "ArcticArcher", "GlacialGiant",
            "WinterWarden", "FrozenFury", "IceIron", "NorthernKnight", "PolarPaladin",
            "FrostFighter", "IceInferno", "SnowSlayer", "ArcticAssassin", "GlacierGladiator",
            "WinterWizard", "FrozenFalcon", "ColdCrusader", "NorthernNinja", "BlizzardBrawler"
        };

        DevvitBridge.LeaderboardEntry[] entries = new DevvitBridge.LeaderboardEntry[count];

        for (int i = 0; i < count; i++)
        {
            entries[i] = new DevvitBridge.LeaderboardEntry
            {
                rank = i + 1,
                username = heroNames[i % heroNames.Length] + (i / heroNames.Length > 0 ? (i / heroNames.Length).ToString() : ""),
                totalPoints = 2000 - (i * 30), // Decreasing points
                avatarUrl = includeAvatars ? GetMockAvatarUrl(i) : ""
            };
        }

        return entries;
    }

    private string GetMockAvatarUrl(int index)
    {
        // Using placeholder avatar service (you can replace with real URLs for testing)
        // These are public placeholder images that work for testing
        string[] avatarUrls = new string[]
        {
            "https://i.pravatar.cc/150?img=1",
            "https://i.pravatar.cc/150?img=2",
            "https://i.pravatar.cc/150?img=3",
            "https://i.pravatar.cc/150?img=4",
            "https://i.pravatar.cc/150?img=5",
            "https://i.pravatar.cc/150?img=6",
            "https://i.pravatar.cc/150?img=7",
            "https://i.pravatar.cc/150?img=8",
            "https://i.pravatar.cc/150?img=9",
            "https://i.pravatar.cc/150?img=10"
        };

        return avatarUrls[index % avatarUrls.Length];
    }

    /// <summary>
    /// Clear the leaderboard (for testing)
    /// </summary>
    public void ClearLeaderboard()
    {
        if (LeaderboardUI.Instance != null)
        {
            LeaderboardUI.Instance.DisplayLeaderboard(new DevvitBridge.LeaderboardEntry[0]);
            Debug.Log("[LeaderboardTestData] Cleared leaderboard");
        }
    }

    /// <summary>
    /// Test with just top 3
    /// </summary>
    public void TestTop3Only()
    {
        DevvitBridge.LeaderboardEntry[] top3 = GenerateMockEntries(3);
        LeaderboardUI.Instance.DisplayLeaderboard(top3);
        Debug.Log("[LeaderboardTestData] Populated top 3 only");
    }

    /// <summary>
    /// Test with top 10
    /// </summary>
    public void TestTop10()
    {
        DevvitBridge.LeaderboardEntry[] top10 = GenerateMockEntries(10);
        LeaderboardUI.Instance.DisplayLeaderboard(top10);
        Debug.Log("[LeaderboardTestData] Populated top 10");
    }
}
