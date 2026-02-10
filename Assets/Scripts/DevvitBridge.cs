using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class DevvitBridge : MonoBehaviour
{
    public static DevvitBridge Instance { get; private set; }

    [Header("User Identity")]
    public string userId;
    public string username;
    public string avatarUrl;

    [Header("Debug")]
    public bool logMessages = true;

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

    // ========== RECEIVING DATA FROM REDDIT ==========

    /// <summary>
    /// Called by Reddit when user identity is available
    /// Message format: { "userId": "t2_abc123", "username": "Player1", "avatarUrl": "https://..." }
    /// </summary>
    public void ReceiveUserData(string json)
    {
        try
        {
            UserData data = JsonUtility.FromJson<UserData>(json);
            userId = data.userId;
            username = data.username;
            avatarUrl = data.avatarUrl;

            if (logMessages)
            {
                Debug.Log($"[DevvitBridge] User identity received: {username} ({userId})");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[DevvitBridge] Error parsing user data: {e.Message}");
        }
    }

    /// <summary>
    /// Called by Reddit with top 50 leaderboard data
    /// Message format: { "entries": [ { "rank": 1, "username": "...", "avatarUrl": "...", "totalPoints": 1000 }, ... ] }
    /// </summary>
    public void ReceiveLeaderboard(string json)
    {
        try
        {
            LeaderboardData data = JsonUtility.FromJson<LeaderboardData>(json);

            if (logMessages)
            {
                Debug.Log($"[DevvitBridge] Leaderboard received: {data.entries.Length} players");
            }

            // Pass to LeaderboardUI to display
            if (LeaderboardUI.Instance != null)
            {
                LeaderboardUI.Instance.DisplayLeaderboard(data.entries);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[DevvitBridge] Error parsing leaderboard: {e.Message}");
        }
    }

    /// <summary>
    /// Called by Reddit with current player's standing
    /// Message format: { "rank": 42, "totalPoints": 550, "levelsCompleted": 3 }
    /// </summary>
    public void ReceivePlayerStanding(string json)
    {
        try
        {
            PlayerStanding data = JsonUtility.FromJson<PlayerStanding>(json);

            if (logMessages)
            {
                Debug.Log($"[DevvitBridge] Player standing: Rank #{data.rank}, {data.totalPoints} points, {data.levelsCompleted} levels");
            }

            // Pass to LeaderboardUI to display
            if (LeaderboardUI.Instance != null)
            {
                LeaderboardUI.Instance.UpdatePlayerStanding(data);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[DevvitBridge] Error parsing player standing: {e.Message}");
        }
    }

    /// <summary>
    /// Called by Reddit with level unlock data and countdown timers
    /// Message format: { "levels": [ { "levelNumber": 0, "isUnlocked": true, "unlockTime": 0, "timeUntilUnlock": 0 }, ... ] }
    /// </summary>
    public void ReceiveUnlockedLevels(string json)
    {
        try
        {
            UnlockedLevelsData data = JsonUtility.FromJson<UnlockedLevelsData>(json);

            if (logMessages)
            {
                Debug.Log($"[DevvitBridge] Received unlock data for {data.levels.Length} levels");
                
                // Log locked levels with countdown
                foreach (var level in data.levels)
                {
                    if (!level.isUnlocked)
                    {
                        long seconds = level.timeUntilUnlock / 1000;
                        long hours = seconds / 3600;
                        long days = hours / 24;
                        Debug.Log($"  Level {level.levelNumber}: Unlocks in {days}d {hours % 24}h");
                    }
                }
            }

            // Notify listeners (e.g., LevelManager)
            OnUnlockDataReceived?.Invoke(data.levels);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DevvitBridge] Error parsing unlock data: {e.Message}");
        }
    }

    /// <summary>
    /// Called when score submission completes (response from backend)
    /// Message format: { "success": true, "heroPoints": 850, "totalPoints": 2500, "rank": 42, "message": "..." }
    /// </summary>
    public void OnScoreSubmitted(string json)
    {
        try
        {
            ScoreSubmissionResponse response = JsonUtility.FromJson<ScoreSubmissionResponse>(json);

            if (response.success)
            {
                if (logMessages)
                {
                    Debug.Log($"[DevvitBridge] Score submitted! Hero Points: {response.heroPoints}, Total: {response.totalPoints}, Rank: #{response.rank}");
                    if (!string.IsNullOrEmpty(response.message))
                    {
                        Debug.Log($"[DevvitBridge] Message: {response.message}");
                    }
                }
                
                // Optionally trigger UI update or achievement notification here
                // Example: EventManager.TriggerEvent("ScoreUpdated", response);
            }
            else
            {
                Debug.LogWarning($"[DevvitBridge] Score submission failed: {response.message}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[DevvitBridge] Error parsing score response: {e.Message}");
        }
    }

    // ========== SENDING DATA TO REDDIT ==========

    /// <summary>
    /// Send level completion data to Reddit backend
    /// </summary>
    public void SendLevelComplete(int levelNumber, int allies, float time, int retries, int points)
    {
        LevelCompleteData data = new LevelCompleteData
        {
            levelNumber = levelNumber,
            alliesSaved = allies,
            timeSpent = time,
            retryCount = retries,
            heroPoints = points
        };

        string json = JsonUtility.ToJson(data);
        SendMessageToReddit("LEVEL_COMPLETE", json);

        if (logMessages)
        {
            Debug.Log($"[DevvitBridge] Sent level complete: Level {levelNumber}, {points} points");
        }
    }

    /// <summary>
    /// Request top 50 leaderboard from Reddit
    /// </summary>
    public void RequestLeaderboard()
    {
        SendMessageToReddit("REQUEST_LEADERBOARD", "{}");

        if (logMessages)
        {
            Debug.Log($"[DevvitBridge] Requested leaderboard");
        }
    }

    /// <summary>
    /// Request current player's standing from Reddit
    /// </summary>
    public void RequestPlayerStanding()
    {
        SendMessageToReddit("REQUEST_PLAYER_STANDING", "{}");

        if (logMessages)
        {
            Debug.Log($"[DevvitBridge] Requested player standing");
        }
    }

    /// <summary>
    /// Request unlocked levels and countdown timers from Reddit
    /// </summary>
    public void RequestUnlockedLevels()
    {
        SendMessageToReddit("REQUEST_UNLOCKED_LEVELS", "{}");

        if (logMessages)
        {
            Debug.Log($"[DevvitBridge] Requested unlocked levels");
        }
    }

    // ========== JAVASCRIPT INTEROP ==========

    /// <summary>
    /// Send message to Reddit parent window (WebGL only)
    /// </summary>
    private void SendMessageToReddit(string messageType, string data)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // In WebGL build, send message to parent window (Reddit)
        string message = $"{{\"type\":\"{messageType}\",\"data\":{data}}}";
        SendToParent(message);
#else
        // In Unity Editor, just log for testing
        Debug.Log($"[DevvitBridge] Would send to Reddit: {messageType} - {data}");
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void SendToParent(string message);
#endif

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
        public int rank;
        public string username;
        public string avatarUrl;
        public int totalPoints;
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
    public class LevelCompleteData
    {
        public int levelNumber;
        public int alliesSaved;
        public float timeSpent;
        public int retryCount;
        public int heroPoints;
    }

    [Serializable]
    public class ScoreSubmissionResponse
    {
        public bool success;
        public int heroPoints;
        public int totalPoints;
        public int rank;
        public string message;
    }

    [Serializable]
    public class LevelUnlockInfo
    {
        public int levelNumber;
        public bool isUnlocked;
        public long unlockTime; // Unix timestamp in milliseconds
        public long timeUntilUnlock; // Milliseconds until unlock (0 if unlocked)
    }

    [Serializable]
    public class UnlockedLevelsData
    {
        public LevelUnlockInfo[] levels;
    }

    // Event for when unlock data is received
    public System.Action<LevelUnlockInfo[]> OnUnlockDataReceived;
}
