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
        return levelNumber <= highestUnlockedLevel;
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

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void CompleteLevel()
    {
        GameManager.Instance.isGameOver = false;
        GameManager.Instance.isLevelCompleted = true;

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

        SceneManager.LoadScene(mainMenu);
    }



}

