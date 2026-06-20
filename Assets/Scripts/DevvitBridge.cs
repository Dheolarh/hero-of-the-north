using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class DevvitBridge : MonoBehaviour
{
    public static DevvitBridge Instance { get; private set; }

    [Header("User Identity (populated from server)")]
    public string userId;
    public string username;
    public string avatarUrl;

    [Header("Debug")]
    public bool logMessages = true;

    // Event for when unlock data is received (subscribed to by LevelManager)
    public System.Action<LevelUnlockInfo[]> OnUnlockDataReceived;

    // ========== LIFECYCLE ==========

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // On start, fetch user identity and level unlock data from the server.
        // In the Unity Editor these will fail gracefully (no Devvit server running locally).
        StartCoroutine(FetchUserIdentity());
        StartCoroutine(FetchUnlockedLevels());
    }

    // ========== FETCHING DATA FROM REDDIT ==========

    /// <summary>
    /// GET /api/user/me — fetches the authenticated Reddit user's identity.
    /// The server resolves userId from the Reddit session, so this is spoofing-proof.
    /// </summary>
    private IEnumerator FetchUserIdentity()
    {
#if UNITY_EDITOR
        userId = "editor_user";
        username = "EditorPlayer";
        avatarUrl = "";
        if (logMessages)
            Debug.Log($"[DevvitBridge] [Editor Mock] User identity set: {username}");
        yield break;
#else
        using UnityWebRequest req = UnityWebRequest.Get("/api/user/me");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[DevvitBridge] Could not fetch user identity: {req.error} (expected in Editor)");
            yield break;
        }

        try
        {
            UserData data = JsonUtility.FromJson<UserData>(req.downloadHandler.text);
            userId   = data.userId;
            username = data.username;
            avatarUrl = data.avatarUrl;

            if (logMessages)
                Debug.Log($"[DevvitBridge] User identity: {username} ({userId})");
        }
        catch (Exception e)
        {
            Debug.LogError($"[DevvitBridge] Error parsing user identity: {e.Message}");
        }
#endif
    }

    /// <summary>
    /// GET /api/levels/all-info — fetches unlock status and countdown timers for all levels.
    /// </summary>
    public void RequestUnlockedLevels()
    {
        StartCoroutine(FetchUnlockedLevels());
    }

    private IEnumerator FetchUnlockedLevels()
    {
#if UNITY_EDITOR
        if (logMessages)
            Debug.Log("[DevvitBridge] [Editor Mock] Mocking unlocked levels.");

        LevelUnlockInfo[] mockLevels = new LevelUnlockInfo[32];
        for (int i = 0; i < mockLevels.Length; i++)
        {
            mockLevels[i] = new LevelUnlockInfo
            {
                levelNumber = i,
                isUnlocked = true,
                unlockTime = 0,
                timeUntilUnlock = 0
            };
        }
        OnUnlockDataReceived?.Invoke(mockLevels);
        yield break;
#else
        using UnityWebRequest req = UnityWebRequest.Get("/api/levels/all-info");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[DevvitBridge] Could not fetch level unlock data: {req.error}");
            yield break;
        }

        try
        {
            UnlockedLevelsData data = JsonUtility.FromJson<UnlockedLevelsData>(req.downloadHandler.text);

            if (logMessages)
                Debug.Log($"[DevvitBridge] Received unlock data for {data.levels.Length} levels");

            OnUnlockDataReceived?.Invoke(data.levels);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DevvitBridge] Error parsing unlock data: {e.Message}");
        }
#endif
    }

    /// <summary>
    /// GET /api/leaderboard/top — fetches top 50 leaderboard entries.
    /// </summary>
    public void RequestLeaderboard()
    {
        StartCoroutine(FetchLeaderboard());
    }

    private IEnumerator FetchLeaderboard()
    {
#if UNITY_EDITOR
        if (logMessages)
            Debug.Log("[DevvitBridge] [Editor Mock] Mocking leaderboard top entries.");
        LeaderboardEntry[] mockEntries = new LeaderboardEntry[5];
        for (int i = 0; i < 5; i++)
        {
            mockEntries[i] = new LeaderboardEntry
            {
                rank = i + 1,
                username = $"Player_{i + 1}",
                userId = $"user_{i + 1}",
                avatarUrl = "",
                totalPoints = 1000 - (i * 100)
            };
        }
        if (LeaderboardUI.Instance != null)
            LeaderboardUI.Instance.DisplayLeaderboard(mockEntries);
        yield break;
#else
        using UnityWebRequest req = UnityWebRequest.Get("/api/leaderboard/top");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[DevvitBridge] Could not fetch leaderboard: {req.error}");
            yield break;
        }

        try
        {
            LeaderboardData data = JsonUtility.FromJson<LeaderboardData>(req.downloadHandler.text);

            if (logMessages)
                Debug.Log($"[DevvitBridge] Leaderboard received: {data.entries.Length} players");

            if (LeaderboardUI.Instance != null)
                LeaderboardUI.Instance.DisplayLeaderboard(data.entries);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DevvitBridge] Error parsing leaderboard: {e.Message}");
        }
#endif
    }

    /// <summary>
    /// GET /api/leaderboard/standing/me — fetches the current player's rank and points.
    /// </summary>
    public void RequestPlayerStanding()
    {
        StartCoroutine(FetchPlayerStanding());
    }

    private IEnumerator FetchPlayerStanding()
    {
#if UNITY_EDITOR
        if (logMessages)
            Debug.Log("[DevvitBridge] [Editor Mock] Mocking player standing.");
        if (LeaderboardUI.Instance != null)
        {
            LeaderboardUI.Instance.UpdatePlayerStanding(new PlayerStanding
            {
                rank = 12,
                totalPoints = 450,
                levelsCompleted = 3
            });
        }
        yield break;
#else
        // Use /me endpoint so the server uses context.userId (no userId in URL needed)
        using UnityWebRequest req = UnityWebRequest.Get("/api/leaderboard/standing/me");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[DevvitBridge] Could not fetch player standing: {req.error}");
            yield break;
        }

        try
        {
            PlayerStandingResponse resp = JsonUtility.FromJson<PlayerStandingResponse>(req.downloadHandler.text);

            if (resp.found)
            {
                if (logMessages)
                    Debug.Log($"[DevvitBridge] Player standing: Rank #{resp.standing.rank}, {resp.standing.totalPoints} pts");

                if (LeaderboardUI.Instance != null)
                    LeaderboardUI.Instance.UpdatePlayerStanding(resp.standing);
            }
            else
            {
                if (logMessages)
                    Debug.Log("[DevvitBridge] Player has no standing yet (no levels completed)");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[DevvitBridge] Error parsing player standing: {e.Message}");
        }
#endif
    }

    // ========== SENDING DATA TO REDDIT ==========

    /// <summary>
    /// POST /api/score/submit — submits level completion data.
    /// The server uses context.userId for authentication — no userId sent from client.
    /// </summary>
    public void SendLevelComplete(int levelNumber, int allies, float time, int retries, int points)
    {
        // Only send game data — server resolves userId from the Reddit session
        LevelCompleteData data = new LevelCompleteData
        {
            levelNumber = levelNumber,
            alliesSaved = allies,
            timeSpent   = time,
            retryCount  = retries
            // Note: heroPoints intentionally omitted — server recalculates it for security
        };

        if (logMessages)
            Debug.Log($"[DevvitBridge] Submitting score: Level {levelNumber}, {allies} allies, {time:F1}s, {retries} retries");

        StartCoroutine(PostScoreSubmission(data));
    }

    private IEnumerator PostScoreSubmission(LevelCompleteData data)
    {
#if UNITY_EDITOR
        if (logMessages)
            Debug.Log("[DevvitBridge] [Editor Mock] Mocking score submission success.");
        
        OnScoreSubmitted(JsonUtility.ToJson(new ScoreSubmissionResponse
        {
            success = true,
            heroPoints = data.alliesSaved * 100 + 200,
            totalPoints = 1500,
            rank = 5,
            message = "Success (Editor Mock)"
        }));
        yield break;
#else
        string json = JsonUtility.ToJson(data);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using UnityWebRequest req = new UnityWebRequest("/api/score/submit", "POST");
        req.uploadHandler   = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[DevvitBridge] Score submission failed: {req.error}");
            OnScoreSubmitted(JsonUtility.ToJson(new ScoreSubmissionResponse
            {
                success = false,
                message = req.error
            }));
            yield break;
        }

        if (logMessages)
            Debug.Log($"[DevvitBridge] Score submitted. Response: {req.downloadHandler.text}");

        OnScoreSubmitted(req.downloadHandler.text);
#endif
    }

    // ========== RESPONSE HANDLERS ==========

    /// <summary>
    /// Called internally when score submission completes.
    /// </summary>
    private void OnScoreSubmitted(string json)
    {
        try
        {
            ScoreSubmissionResponse response = JsonUtility.FromJson<ScoreSubmissionResponse>(json);

            if (response.success)
            {
                if (logMessages)
                    Debug.Log($"[DevvitBridge] Score accepted! Hero Points: {response.heroPoints}, Total: {response.totalPoints}, Rank: #{response.rank}");
            }
            else
            {
                Debug.LogWarning($"[DevvitBridge] Score rejected: {response.message}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[DevvitBridge] Error parsing score response: {e.Message}");
        }
    }

    // ========== DATA STRUCTURES ==========

    [Serializable]
    public class UserData
    {
        public string userId;
        public string username;
        public string avatarUrl;
    }

    [Serializable]
    public class LeaderboardEntry
    {
        public int    rank;
        public string username;
        public string userId;
        public string avatarUrl;
        public int    totalPoints;
    }

    [Serializable]
    public class LeaderboardData
    {
        public LeaderboardEntry[] entries;
    }

    [Serializable]
    public class PlayerStanding
    {
        public int rank;
        public int totalPoints;
        public int levelsCompleted;
    }

    [Serializable]
    public class PlayerStandingResponse
    {
        public bool           found;
        public PlayerStanding standing;
    }

    [Serializable]
    public class LevelCompleteData
    {
        public int   levelNumber;
        public int   alliesSaved;
        public float timeSpent;
        public int   retryCount;
        // heroPoints NOT sent — server recalculates to prevent cheating
    }

    [Serializable]
    public class ScoreSubmissionResponse
    {
        public bool   success;
        public int    heroPoints;
        public int    totalPoints;
        public int    rank;
        public string message;
    }

    [Serializable]
    public class LevelUnlockInfo
    {
        public int  levelNumber;
        public bool isUnlocked;
        public long unlockTime;       // Unix ms timestamp when this level unlocks
        public long timeUntilUnlock;  // Milliseconds remaining (0 if already unlocked)
    }

    [Serializable]
    public class UnlockedLevelsData
    {
        public LevelUnlockInfo[] levels;
    }
}
