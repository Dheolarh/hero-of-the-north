using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    // Events for observer pattern
    public event System.Action<int> OnAlliesSavedChanged;
    public event System.Action<int> OnRetryCountChanged;

    [Header("Current Level Stats")]
    public int currentLevelNumber;
    
    private int _alliesSaved;
    public int alliesSaved
    {
        get => _alliesSaved;
        set
        {
            _alliesSaved = value;
            OnAlliesSavedChanged?.Invoke(_alliesSaved);
        }
    }
    
    public float timeSpent;
    
    private int _retryCount;
    public int retryCount
    {
        get => _retryCount;
        set
        {
            _retryCount = value;
            OnRetryCountChanged?.Invoke(_retryCount);
        }
    }
    
    public int heroPoints;

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

    void Update()
    {
        if (currentLevelNumber > 0 && !GameManager.Instance.isGameOver && !GameManager.Instance.isLevelCompleted)
        {
            timeSpent += Time.unscaledDeltaTime;
        }
    }

    public void ResetForNewLevel(int levelNumber)
    {
        currentLevelNumber = levelNumber;
        alliesSaved = 0;
        timeSpent = 0f;
        retryCount = 0;
        heroPoints = 0;

        Debug.Log($"[ScoreManager] Reset for Level {levelNumber}");
    }

    public void IncrementRetryCount()
    {
        retryCount++;

        alliesSaved = 0;
        Debug.Log($"[ScoreManager] Retry {retryCount} - Time so far: {timeSpent:F2}s - Allies reset to 0");
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool isGameLevel = scene.name != "Main" && scene.name != "Tutorial";

        if (isGameLevel)
        {
            if (retryCount == 0)
            {
                Debug.Log($"[ScoreManager] Game level '{scene.name}' loaded - Timer started");
            }
            else
            {
                Debug.Log($"[ScoreManager] Level reloaded on retry - Continuing from {timeSpent:F2}s");
            }
        }
        else
        {
            currentLevelNumber = 0;
            Debug.Log($"[ScoreManager] Non-game scene '{scene.name}' loaded - Stats tracking paused");
        }
    }

    public void CalculateHeroPoints()
    {
        int alliesPoints = alliesSaved * 100;
        float timePoints = Mathf.Max(0, 300 - timeSpent);
        float retryPenalty = retryCount * 5;
        heroPoints = Mathf.RoundToInt(alliesPoints + timePoints - retryPenalty);

        Debug.Log($"[ScoreManager] Level {currentLevelNumber} Complete!");
        Debug.Log($"  Allies Saved: {alliesSaved} ({alliesPoints} pts)");
        Debug.Log($"  Time: {timeSpent:F2}s ({timePoints} pts)");
        Debug.Log($"  Retries: {retryCount} (-{retryPenalty} pts)");
        Debug.Log($"  HERO POINTS: {heroPoints}");
    }
}