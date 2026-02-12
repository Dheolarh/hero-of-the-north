using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level Configuration")]
    [SerializeField] private List<LevelData> allLevels = new List<LevelData>();

    private int currentLevelIndex = 0;
    private int highestUnlockedLevel = 1;
    private DevvitBridge.LevelUnlockInfo[] serverUnlockData;
    public string mainMenu;

    public bool allowMultiJumps = false;

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

        // Subscribe to unlock data from DevvitBridge
        if (DevvitBridge.Instance != null)
        {
            DevvitBridge.Instance.OnUnlockDataReceived += OnUnlockDataReceived;
        }
    }

    void Start()
    {
        // Request unlock data from server on start
        if (DevvitBridge.Instance != null)
        {
            DevvitBridge.Instance.RequestUnlockedLevels();
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from unlock data
        if (DevvitBridge.Instance != null)
        {
            DevvitBridge.Instance.OnUnlockDataReceived -= OnUnlockDataReceived;
        }
    }

    /// <summary>
    /// Called when unlock data is received from server
    /// </summary>
    private void OnUnlockDataReceived(DevvitBridge.LevelUnlockInfo[] levels)
    {
        serverUnlockData = levels;

        // Update highest unlocked level based on server data
        highestUnlockedLevel = 0;
        foreach (var level in levels)
        {
            if (level.isUnlocked)
            {
                highestUnlockedLevel = level.levelNumber + 1; // +1 because we track "highest + 1"
            }
        }

        Debug.Log($"[LevelManager] Server unlock data received. Highest unlocked: Level {highestUnlockedLevel - 1}");
    }
    

    public LevelData GetLevel(int levelNumber)
    {
        return allLevels.Find(level => level.levelNumber == levelNumber);
    }

    public List<LevelData> GetAllLevels()
    {
        return allLevels;
    }

    public bool IsLevelUnlocked(int levelNumber)
    {
        // If we have server data, use it
        if (serverUnlockData != null && levelNumber < serverUnlockData.Length)
        {
            return serverUnlockData[levelNumber].isUnlocked;
        }

        // Fallback to local tracking (for offline/editor testing)
        return levelNumber <= highestUnlockedLevel;
    }

    /// <summary>
    /// Get unlock info for a specific level (for countdown timer)
    /// </summary>
    public DevvitBridge.LevelUnlockInfo GetLevelUnlockInfo(int levelNumber)
    {
        if (serverUnlockData != null && levelNumber < serverUnlockData.Length)
        {
            return serverUnlockData[levelNumber];
        }

        return null;
    }

    public void LoadLevel(int levelNumber)
    {
        GameManager.Instance.isGameOver = false;
        LevelData level = GetLevel(levelNumber);
        if (level != null && IsLevelUnlocked(levelNumber))
        {
            currentLevelIndex = levelNumber;

            // Reset score stats for new level
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ResetForNewLevel(levelNumber);
            }

            SceneManager.LoadScene(level.sceneName);
        }
        else
        {
            Debug.LogWarning($"Level {levelNumber} is locked or doesn't exist!");
        }
    }

    public void RestartLevel()
    {
        GameManager.Instance.isGameOver = false;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.IncrementRetryCount();
        }
        UIManager.Instance.HidePanels();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void CompleteLevel()
    {
        GameManager.Instance.isGameOver = false;
        GameManager.Instance.isLevelCompleted = true;
        AudioManager.Instance.PlaySfx("Success");
        GameManager.Instance.PauseGame();
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.CalculateHeroPoints();

            // Send score data to Reddit backend
            if (DevvitBridge.Instance != null)
            {
                DevvitBridge.Instance.SendLevelComplete(
                    ScoreManager.Instance.currentLevelNumber,
                    ScoreManager.Instance.alliesSaved,
                    ScoreManager.Instance.timeSpent,
                    ScoreManager.Instance.retryCount,
                    ScoreManager.Instance.heroPoints
                );
            }
        }
        UIManager.Instance.ToggleLevelCompleteUI();
        StartCoroutine(AfterLevelComplete());
    }

    IEnumerator AfterLevelComplete()
    {
        yield return new WaitForSecondsRealtime(2f);
        UIManager.Instance.HidePanels();
        SceneManager.LoadScene(mainMenu);
    }
}


