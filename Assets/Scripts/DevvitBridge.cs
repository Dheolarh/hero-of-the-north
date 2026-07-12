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

    /// <summary>Fired as soon as the username has been fetched and trimmed.</summary>
    public System.Action<string> OnUsernameReady;

    // ========== LIFECYCLE ==========

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // Fetch identity ASAP in Awake so username is available before any scene UI opens
            StartCoroutine(FetchUserIdentity());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Level unlock data can wait until Start — it doesn't block the tutorial UI
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
        username = TrimUsername("Player");
        avatarUrl = "";
        if (logMessages)
            Debug.Log($"[DevvitBridge] [Editor Mock] User identity set: {username}");
        OnUsernameReady?.Invoke(username);
        if (LeaderboardUI.Instance != null)
            LeaderboardUI.Instance.UpdatePlayerStandingNameAndAvatar();
        yield break;
#else
        string url = "/api/user/me?t=" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        using UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[DevvitBridge] Could not fetch user identity: {req.error} (expected in Editor)");
            yield break;
        }

        try
        {
            UserData data = JsonUtility.FromJson<UserData>(req.downloadHandler.text);
            userId    = data.userId;
            username  = TrimUsername(data.username);
            avatarUrl = data.avatarUrl;

            if (logMessages)
                Debug.Log($"[DevvitBridge] User identity: {username} ({userId})");

            // Notify all listeners (e.g. TutorialTextInjector) immediately
            OnUsernameReady?.Invoke(username);

            if (LeaderboardUI.Instance != null)
            {
                LeaderboardUI.Instance.UpdatePlayerStandingNameAndAvatar();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[DevvitBridge] Error parsing user identity: {e.Message}");
        }
#endif
    }

    /// <summary>
    /// Explicitly request the user identity details from Reddit.
    /// </summary>
    public void RequestUserIdentity()
    {
        StartCoroutine(FetchUserIdentity());
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

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        LevelUnlockInfo[] mockLevels = new LevelUnlockInfo[32];
        for (int i = 0; i < mockLevels.Length; i++)
        {
            // Simulate: Tutorial (0) and Level 1 (1) are unlocked initially.
            // Levels 2+ unlock every 24 hours (1 day per level).
            bool isUnlocked = (i < 2);
            long timeRemaining = isUnlocked ? 0 : (i - 1) * 24 * 60 * 60 * 1000;

            mockLevels[i] = new LevelUnlockInfo
            {
                levelNumber = i,
                isUnlocked = isUnlocked,
                unlockTime = now + timeRemaining,
                timeUntilUnlock = timeRemaining
            };
        }
        OnUnlockDataReceived?.Invoke(mockLevels);
        yield break;
#else
        string url = "/api/levels/all-info?t=" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        using UnityWebRequest req = UnityWebRequest.Get(url);
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
        string url = "/api/leaderboard/top?t=" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        using UnityWebRequest req = UnityWebRequest.Get(url);
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
        string url = "/api/leaderboard/standing/me?t=" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        using UnityWebRequest req = UnityWebRequest.Get(url);
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
                    Debug.Log($"[DevvitBridge] Player standing: Rank {resp.standing.rank}, {resp.standing.totalPoints} pts");

                if (LeaderboardUI.Instance != null)
                    LeaderboardUI.Instance.UpdatePlayerStanding(resp.standing);
            }
            else
            {
                if (logMessages)
                    Debug.Log("[DevvitBridge] Player has no standing yet (no levels completed)");

                if (LeaderboardUI.Instance != null)
                {
                    PlayerStanding fallback = new PlayerStanding { rank = 0, totalPoints = 0, levelsCompleted = 0 };
                    LeaderboardUI.Instance.UpdatePlayerStanding(fallback);
                }
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
                    Debug.Log($"[DevvitBridge] Score accepted! Hero Points: {response.heroPoints}, Total: {response.totalPoints}, Rank: {response.rank}");

                // Auto-refresh leaderboard so the player sees their new rank/points
                // without needing to quit and re-launch the game
                StartCoroutine(RefreshLeaderboardAfterDelay(1.5f));
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

    /// <summary>
    /// Waits a short moment (for the server to commit the score) then re-fetches
    /// the leaderboard and the player's personal standing.
    /// </summary>
    private IEnumerator RefreshLeaderboardAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (logMessages)
            Debug.Log("[DevvitBridge] Auto-refreshing leaderboard after score submission...");
        RequestLeaderboard();
    }

    /// <summary>
    /// GET /api/levels/community — fetches all levels published by the community.
    /// </summary>
    public void RequestCommunityLevels(System.Action<CommunityLevelInfo[]> onReceived)
    {
        StartCoroutine(FetchCommunityLevels(onReceived));
    }

    private IEnumerator FetchCommunityLevels(System.Action<CommunityLevelInfo[]> onReceived)
    {
#if UNITY_EDITOR
        if (logMessages)
            Debug.Log("[DevvitBridge] [Editor Mock] Mocking community levels.");

        CommunityLevelInfo[] mockLevels = new CommunityLevelInfo[3];
        mockLevels[0] = new CommunityLevelInfo
        {
            id = "post_1",
            levelName = "Winter Parkour",
            creator = "Redditor_Alpha",
            playCount = 12,
            topPlayer = "HeroPlayer",
            avatarUrl = "https://www.redditstatic.com/avatars/defaults/v2/avatar_default_0.png",
            levelData = "{\"levelName\":\"Winter Parkour\",\"creator\":\"Redditor_Alpha\",\"gridWidth\":32,\"gridHeight\":18,\"playerStartPos\":{\"x\":5,\"y\":5},\"hasPlayerStart\":true,\"goalPos\":{\"x\":25,\"y\":5},\"hasGoal\":true,\"tiles\":[{\"type\":\"Floor\",\"position\":{\"x\":5,\"y\":4},\"scale\":{\"x\":1,\"y\":1},\"rotation\":0}],\"traps\":[]}"
        };
        mockLevels[1] = new CommunityLevelInfo
        {
            id = "post_2",
            levelName = "Spike Valley",
            creator = "Redditor_Beta",
            playCount = 45,
            topPlayer = "Speedrunner",
            avatarUrl = "", // Empty to trigger fallback avatar
            levelData = "{\"levelName\":\"Spike Valley\",\"creator\":\"Redditor_Beta\",\"gridWidth\":32,\"gridHeight\":18,\"playerStartPos\":{\"x\":5,\"y\":5},\"hasPlayerStart\":true,\"goalPos\":{\"x\":25,\"y\":5},\"hasGoal\":true,\"tiles\":[{\"type\":\"Floor\",\"position\":{\"x\":5,\"y\":4},\"scale\":{\"x\":1,\"y\":1},\"rotation\":0}],\"traps\":[]}"
        };
        mockLevels[2] = new CommunityLevelInfo
        {
            id = "post_3",
            levelName = "Ice Slopes",
            creator = "Redditor_Gamma",
            playCount = 5,
            topPlayer = "NorthHero",
            avatarUrl = "https://www.redditstatic.com/avatars/defaults/v2/avatar_default_1.png",
            levelData = "{\"levelName\":\"Ice Slopes\",\"creator\":\"Redditor_Gamma\",\"gridWidth\":32,\"gridHeight\":18,\"playerStartPos\":{\"x\":5,\"y\":5},\"hasPlayerStart\":true,\"goalPos\":{\"x\":25,\"y\":5},\"hasGoal\":true,\"tiles\":[{\"type\":\"Floor\",\"position\":{\"x\":5,\"y\":4},\"scale\":{\"x\":1,\"y\":1},\"rotation\":0}],\"traps\":[]}"
        };
        onReceived?.Invoke(mockLevels);
        yield break;
#else
        string url = "/api/levels/community?t=" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        using UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[DevvitBridge] Could not fetch community levels: {req.error}");
            onReceived?.Invoke(new CommunityLevelInfo[0]);
            yield break;
        }

        try
        {
            CommunityLevelsResponse resp = JsonUtility.FromJson<CommunityLevelsResponse>(req.downloadHandler.text);
            onReceived?.Invoke(resp.levels ?? new CommunityLevelInfo[0]);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DevvitBridge] Error parsing community levels response: {e.Message}");
            onReceived?.Invoke(new CommunityLevelInfo[0]);
        }
#endif
    }

    // ========== UTILITIES ==========

    /// <summary>
    /// Strips any leading "u/" prefix Reddit sometimes includes in usernames.
    /// Returns "hero" if the name is null or empty (used as a safe fallback).
    /// </summary>
    public static string TrimUsername(string raw, string fallback = "hero")
    {
        if (string.IsNullOrWhiteSpace(raw))
            return fallback;

        // Strip leading u/ or U/ (case-insensitive)
        if (raw.StartsWith("u/", System.StringComparison.OrdinalIgnoreCase))
            raw = raw.Substring(2);

        return string.IsNullOrWhiteSpace(raw) ? fallback : raw;
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

    [Serializable]
    public class CommunityLevelInfo
    {
        public string id;
        public string levelName;
        public string creator;
        public string levelData;
        public int playCount;
        public string topPlayer;
        public string avatarUrl;
    }

    [Serializable]
    public class CommunityLevelsResponse
    {
        public CommunityLevelInfo[] levels;
    }
}
