using UnityEngine;

public class LeaderboardMockPopulator : MonoBehaviour
{
    [Header("Mock Settings")]
    [Tooltip("Number of entries to generate")]
    [SerializeField] private int numberOfEntries = 50;

    [Tooltip("If checked, the leaderboard will automatically populate on Start in Play Mode")]
    [SerializeField] private bool populateOnStart = true;

    [Tooltip("Keyboard shortcut to trigger populating the leaderboard in Play Mode")]
    [SerializeField] private KeyCode triggerKey = KeyCode.L;

    void Start()
    {
        if (populateOnStart)
        {
            PopulateMockData();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(triggerKey))
        {
            PopulateMockData();
        }
    }

    /// <summary>
    /// Populates the LeaderboardUI with generated mock entries.
    /// Right-click the component in the Inspector during Play Mode and select "Populate Mock Leaderboard" to run this.
    /// </summary>
    [ContextMenu("Populate Mock Leaderboard")]
    public void PopulateMockData()
    {
        if (LeaderboardUI.Instance == null)
        {
            Debug.LogError("[LeaderboardMockPopulator] LeaderboardUI.Instance is null! Make sure the Leaderboard UI canvas is active in the scene.");
            return;
        }

        Debug.Log($"[LeaderboardMockPopulator] Generating {numberOfEntries} mock leaderboard entries...");

        DevvitBridge.LeaderboardEntry[] mockEntries = new DevvitBridge.LeaderboardEntry[numberOfEntries];
        for (int i = 0; i < numberOfEntries; i++)
        {
            mockEntries[i] = new DevvitBridge.LeaderboardEntry
            {
                rank = i + 1,
                username = GetMockUsername(i),
                userId = $"user_{i + 1}",
                avatarUrl = "", // Set empty so the default avatar image is used
                totalPoints = (numberOfEntries - i) * 125 + Random.Range(10, 90)
            };
        }

        // Display the mock entries in LeaderboardUI
        LeaderboardUI.Instance.DisplayLeaderboard(mockEntries);

        // Also mock the player standing info
        LeaderboardUI.Instance.UpdatePlayerStanding(new DevvitBridge.PlayerStanding
        {
            rank = Random.Range(1, numberOfEntries + 10),
            totalPoints = Random.Range(100, numberOfEntries * 125),
            levelsCompleted = Random.Range(1, 10)
        });

        Debug.Log("[LeaderboardMockPopulator] Mock leaderboard populated successfully!");
    }

    private string GetMockUsername(int index)
    {
        string[] adjectives = { "Valkyrie", "Viking", "Berserker", "Thor", "Odin", "Loki", "Freya", "Frost", "Ragnarok", "Saga", "Skadi", "Fenrir" };
        string[] nouns = { "Slayer", "Warrior", "Shield", "Hunter", "Chieftain", "Seer", "Rider", "Skald", "Thane", "Jarl", "Wolf", "Raven" };

        if (index < 3)
        {
            string[] topNames = { "The_Allfather", "Shield_Maiden", "Thunder_God" };
            return topNames[index];
        }

        string adj = adjectives[Random.Range(0, adjectives.Length)];
        string noun = nouns[Random.Range(0, nouns.Length)];
        int num = Random.Range(10, 999);

        return $"{adj}{noun}_{num}";
    }
}
