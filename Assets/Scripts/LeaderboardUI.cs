using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    public static LeaderboardUI Instance { get; private set; }

    [Header("Leaderboard List (Top 50)")]
    [Tooltip("The Top 3 entries (already in scene, will be updated in place)")]
    [SerializeField] private GameObject[] top3Entries = new GameObject[3];

    [Tooltip("Container for ranks 4-50 (dynamic spawning)")]
    [SerializeField] private Transform leaderboardContainer;
    [SerializeField] private GameObject leaderboardEntryPrefab;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Player Standing Panel")]
    [SerializeField] private GameObject playerStandingPanel;
    [SerializeField] private TextMeshProUGUI playerRankText;
    [SerializeField] private Image playerHeroImage;
    [SerializeField] private TextMeshProUGUI playerHeroName;
    [SerializeField] private TextMeshProUGUI playerPointsText;

    [Header("Loading State")]
    [SerializeField] private GameObject loadingSpinner;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Settings")]
    [SerializeField] private bool autoRefreshOnEnable = true;

    private Dictionary<string, Sprite> avatarCache = new Dictionary<string, Sprite>();
    private List<GameObject> dynamicLeaderboardEntries = new List<GameObject>();

    void Awake()
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

    void OnEnable()
    {
        if (autoRefreshOnEnable)
        {
            RefreshLeaderboard();
        }
    }

    /// <summary>
    /// Request fresh leaderboard data from Reddit
    /// </summary>
    public void RefreshLeaderboard()
    {
        if (DevvitBridge.Instance != null)
        {
            SetLoadingState(true, "Loading leaderboard...");
            DevvitBridge.Instance.RequestLeaderboard();
            DevvitBridge.Instance.RequestPlayerStanding();
        }
        else
        {
            SetLoadingState(false, "DevvitBridge not found!");
            Debug.LogError("[LeaderboardUI] DevvitBridge.Instance is null!");
        }
    }

    /// <summary>
    /// Display leaderboard entries (called by DevvitBridge)
    /// </summary>
    public void DisplayLeaderboard(DevvitBridge.LeaderboardEntry[] entries)
    {
        // Clear existing dynamic entries (ranks 4-50)
        ClearDynamicEntries();

        if (entries == null || entries.Length == 0)
        {
            SetLoadingState(false, "No leaderboard data available");
            return;
        }

        // Update top 3 (fixed positions in scene)
        for (int i = 0; i < Mathf.Min(3, entries.Length); i++)
        {
            if (i < top3Entries.Length && top3Entries[i] != null)
            {
                LeaderboardEntry entryScript = top3Entries[i].GetComponent<LeaderboardEntry>();
                if (entryScript != null)
                {
                    entryScript.SetData(entries[i].rank, entries[i].username, entries[i].totalPoints);

                    // Download avatar if URL is provided
                    if (!string.IsNullOrEmpty(entries[i].avatarUrl))
                    {
                        StartCoroutine(LoadAvatar(entries[i].avatarUrl, entryScript));
                    }
                }
            }
        }

        // Create entries for ranks 4-50 (dynamic spawning under Rankings)
        for (int i = 3; i < entries.Length; i++)
        {
            GameObject entryObj = Instantiate(leaderboardEntryPrefab, leaderboardContainer);
            LeaderboardEntry entryScript = entryObj.GetComponent<LeaderboardEntry>();

            if (entryScript != null)
            {
                entryScript.SetData(entries[i].rank, entries[i].username, entries[i].totalPoints);

                // Download avatar if URL is provided
                if (!string.IsNullOrEmpty(entries[i].avatarUrl))
                {
                    StartCoroutine(LoadAvatar(entries[i].avatarUrl, entryScript));
                }
            }

            dynamicLeaderboardEntries.Add(entryObj);
        }

        SetLoadingState(false, "");
        Debug.Log($"[LeaderboardUI] Displayed {entries.Length} leaderboard entries (Top 3 + {entries.Length - 3} dynamic)");
    }

    /// <summary>
    /// Update player standing panel (called by DevvitBridge)
    /// </summary>
    public void UpdatePlayerStanding(DevvitBridge.PlayerStanding standing)
    {
        if (playerStandingPanel != null)
        {
            playerStandingPanel.SetActive(true);
        }

        if (playerRankText != null)
        {
            playerRankText.text = $"#{standing.rank}";
        }

        if (playerHeroName != null && DevvitBridge.Instance != null)
        {
            playerHeroName.text = DevvitBridge.Instance.username;
        }

        if (playerPointsText != null)
        {
            playerPointsText.text = $"{standing.totalPoints}";
        }

        // Load player's avatar
        if (playerHeroImage != null && DevvitBridge.Instance != null && !string.IsNullOrEmpty(DevvitBridge.Instance.avatarUrl))
        {
            StartCoroutine(LoadPlayerAvatar(DevvitBridge.Instance.avatarUrl));
        }

        Debug.Log($"[LeaderboardUI] Updated player standing: Rank #{standing.rank}, {standing.totalPoints} pts");
    }

    private IEnumerator LoadPlayerAvatar(string url)
    {
        // Check cache first
        if (avatarCache.ContainsKey(url))
        {
            playerHeroImage.sprite = avatarCache[url];
            yield break;
        }

        // Download image
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );

                // Cache it
                avatarCache[url] = sprite;

                // Set it
                playerHeroImage.sprite = sprite;
            }
            else
            {
                Debug.LogWarning($"[LeaderboardUI] Failed to load player avatar: {url}");
            }
        }
    }

    /// <summary>
    /// Download and cache profile picture
    /// </summary>
    private IEnumerator LoadAvatar(string url, LeaderboardEntry entry)
    {
        // Check cache first
        if (avatarCache.ContainsKey(url))
        {
            entry.SetAvatar(avatarCache[url]);
            yield break;
        }

        // Download image
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );

                // Cache it
                avatarCache[url] = sprite;

                // Set it
                entry.SetAvatar(sprite);
            }
            else
            {
                Debug.LogWarning($"[LeaderboardUI] Failed to load avatar: {url}");
            }
        }
    }

    /// <summary>
    /// Clear dynamic leaderboard entries (ranks 4-50 only, keep top 3)
    /// </summary>
    private void ClearDynamicEntries()
    {
        foreach (var entry in dynamicLeaderboardEntries)
        {
            Destroy(entry);
        }
        dynamicLeaderboardEntries.Clear();
    }

    /// <summary>
    /// Show/hide loading state
    /// </summary>
    private void SetLoadingState(bool isLoading, string message)
    {
        if (loadingSpinner != null)
        {
            loadingSpinner.SetActive(isLoading);
        }

        if (statusText != null)
        {
            statusText.text = message;
            statusText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }
    }
}
