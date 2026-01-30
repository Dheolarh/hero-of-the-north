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
    
    public bool allowMultiJumps = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgress();
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
        LevelData level = GetLevel(levelNumber);
        if (level != null && IsLevelUnlocked(levelNumber))
        {
            currentLevelIndex = levelNumber;
            SceneManager.LoadScene(level.sceneName);
        }
        else
        {
            Debug.LogWarning($"Level {levelNumber} is locked or doesn't exist!");
        }
    }

    public void LoadNextLevel()
    {
        int nextLevel = currentLevelIndex + 1;
        if (nextLevel <= allLevels.Count)
        {
            LoadLevel(nextLevel);
        }
        else
        {
            Debug.Log("No more levels! Game complete!");
        }
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void CompleteLevel()
    {
        int nextLevel = currentLevelIndex + 1;
        if (nextLevel > highestUnlockedLevel)
        {
            highestUnlockedLevel = nextLevel;
            SaveProgress();
        }
    }

    public int GetCurrentLevelNumber()
    {
        return currentLevelIndex;
    }

    public int GetHighestUnlockedLevel()
    {
        return highestUnlockedLevel;
    }

    private void SaveProgress()
    {
        PlayerPrefs.SetInt("HighestUnlockedLevel", highestUnlockedLevel);
        PlayerPrefs.Save();
    }

    private void LoadProgress()
    {
        highestUnlockedLevel = PlayerPrefs.GetInt("HighestUnlockedLevel", 1);
    }

    public void ResetProgress()
    {
        highestUnlockedLevel = 1;
        SaveProgress();
    }
}
