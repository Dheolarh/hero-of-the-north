using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level Configuration")]
    [SerializeField] private List<LevelData> allLevels = new List<LevelData>();

    [Header("Scene Names")]
    public string mainMenu = "Main";
    [Tooltip("The dedicated empty scene for gameplay. Keep game UI here, separate from Main menu UI.")]
    public string gameScene = "Game";

    // ── Runtime state ──────────────────────────────────────────────────────
    private int          _currentLevelIndex = 0;
    private int          _highestUnlockedLevel = 1;
    private LevelData    _currentLevelData;
    private GameObject   _currentLevelInstance;

    private DevvitBridge.LevelUnlockInfo[] _serverUnlockData;

    /// <summary>The LevelData for the level currently loaded/playing.</summary>
    public LevelData CurrentLevelData => _currentLevelData;

    // ── Events ─────────────────────────────────────────────────────────────
    /// <summary>Fired when DevvitBridge receives unlock data from the server.</summary>
    public System.Action<DevvitBridge.LevelUnlockInfo[]> OnUnlockDataReceived;

    // ── Lifecycle ──────────────────────────────────────────────────────────

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
            return;
        }

        if (DevvitBridge.Instance != null)
            DevvitBridge.Instance.OnUnlockDataReceived += OnServerUnlockDataReceived;
    }

    void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnDestroy()
    {
        if (DevvitBridge.Instance != null)
            DevvitBridge.Instance.OnUnlockDataReceived -= OnServerUnlockDataReceived;
    }

    void Start()
    {
        if (DevvitBridge.Instance != null)
            DevvitBridge.Instance.RequestUnlockedLevels();
    }

    /// <summary>
    /// When the Game scene finishes loading, automatically spawn the queued level prefab.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[LevelManager] OnSceneLoaded: Loaded scene '{scene.name}', currentLevelData={(_currentLevelData != null ? _currentLevelData.levelName : "null")}");
        if (scene.name == gameScene)
        {
            if (_currentLevelData != null)
            {
                SpawnLevel();
            }
            else
            {
                Debug.LogWarning("[LevelManager] OnSceneLoaded: _currentLevelData is NULL, cannot spawn level!");
            }
        }
    }

    // ── Server unlock data ─────────────────────────────────────────────────

    private void OnServerUnlockDataReceived(DevvitBridge.LevelUnlockInfo[] levels)
    {
        _serverUnlockData = levels;

        _highestUnlockedLevel = 0;
        foreach (var level in levels)
        {
            if (level.isUnlocked)
                _highestUnlockedLevel = level.levelNumber + 1;
        }

        Debug.Log($"[LevelManager] Unlock data received. Highest unlocked: Level {_highestUnlockedLevel - 1}");

        // Notify subscribers (like LevelSelectUI) that new unlock data has been received
        OnUnlockDataReceived?.Invoke(levels);
    }

    // ── Public queries ─────────────────────────────────────────────────────

    public LevelData GetLevel(int levelNumber)
        => allLevels.Find(l => l.levelNumber == levelNumber);

    public List<LevelData> GetAllLevels() => allLevels;

    public bool IsLevelUnlocked(int levelNumber)
    {
        // If the level has no prefab assigned, it is not created/ready yet
        LevelData level = GetLevel(levelNumber);
        if (level == null || level.levelPrefab == null)
            return false;

        if (_serverUnlockData != null && levelNumber < _serverUnlockData.Length)
            return _serverUnlockData[levelNumber].isUnlocked;

        return levelNumber <= _highestUnlockedLevel;
    }

    public DevvitBridge.LevelUnlockInfo GetLevelUnlockInfo(int levelNumber)
    {
        if (_serverUnlockData != null && levelNumber < _serverUnlockData.Length)
            return _serverUnlockData[levelNumber];
        return null;
    }

    // ── Level loading ──────────────────────────────────────────────────────

    /// <summary>
    /// Load a level by number. Loads the Game scene (if not already in it),
    /// then instantiates the level's prefab.
    /// </summary>
    public void LoadLevel(int levelNumber)
    {
        LevelData level = GetLevel(levelNumber);

        if (level == null)
        {
            Debug.LogWarning($"[LevelManager] Level {levelNumber} not found in allLevels list!");
            return;
        }

        if (!IsLevelUnlocked(levelNumber))
        {
            Debug.LogWarning($"[LevelManager] Level {levelNumber} is locked!");
            return;
        }

        if (level.levelPrefab == null)
        {
            Debug.LogError($"[LevelManager] Level {levelNumber} ({level.levelName}) has no prefab assigned!");
            return;
        }

        // Reset state
        GameManager.Instance.isGameOver      = false;
        GameManager.Instance.isLevelCompleted = false;

        _currentLevelIndex = levelNumber;
        _currentLevelData  = level;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.ResetForNewLevel(levelNumber);

        // Load the Game scene — SpawnLevel() fires automatically in OnSceneLoaded
        if (SceneManager.GetActiveScene().name == gameScene)
            SpawnLevel();   // already in Game scene (e.g. restarting from Game scene somehow)
        else
            SceneManager.LoadScene(gameScene);
    }

    /// <summary>
    /// Restart the current level — destroys and re-instantiates the prefab
    /// WITHOUT reloading the scene (much faster than a full scene load).
    /// </summary>
    public void RestartLevel()
    {
        if (_currentLevelData == null)
        {
            Debug.LogWarning("[LevelManager] RestartLevel called but no level is loaded!");
            return;
        }

        // Always reset time scale — if the player retried from the pause menu,
        // timeScale is still 0 and the game would appear frozen after restart
        Time.timeScale = 1f;
        GameManager.Instance.isPaused        = false;
        GameManager.Instance.isGameOver      = false;
        GameManager.Instance.isLevelCompleted = false;

        if (AudioManager.Instance != null)
            AudioManager.Instance.StopAllSoundsExceptMusic();

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.IncrementRetryCount();

        UIManager.Instance.HidePanels();

        SpawnLevel(); // Re-instantiate prefab in place
    }

    /// <summary>
    /// Instantiates the current level's prefab, wires the camera to the spawned player.
    /// Destroys the previous instance if one exists.
    /// </summary>
    private void SpawnLevel()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopAllSoundsExceptMusic();

        // Destroy previous level instance
        if (_currentLevelInstance != null)
        {
            Destroy(_currentLevelInstance);
            _currentLevelInstance = null;
        }

        // Spawn new level
        _currentLevelInstance = Instantiate(_currentLevelData.levelPrefab);
        _currentLevelInstance.name = $"Level_{_currentLevelData.levelNumber}_{_currentLevelData.levelName}";

        // Wire camera to the player inside the spawned prefab
        PlayerController player = _currentLevelInstance.GetComponentInChildren<PlayerController>();
        if (player != null)
        {
            CameraFollow cam = FindFirstObjectByType<CameraFollow>();
            if (cam != null)
                cam.SetTarget(player.transform);
            else
                Debug.LogWarning("[LevelManager] No CameraFollow found in scene!");
        }
        else
        {
            Debug.LogWarning("[LevelManager] No PlayerController found inside level prefab!");
        }

        // Show HUD
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HidePanels();
            UIManager.Instance.SetHUDActive(true);
        }

        Debug.Log($"[LevelManager] Spawned: Level {_currentLevelData.levelNumber} — {_currentLevelData.levelName}");
    }

    // ── Level completion ───────────────────────────────────────────────────

    public void CompleteLevel()
    {
        if (GameManager.Instance.isLevelCompleted) return;

        Debug.Log("[LevelManager] CompleteLevel called.");

        GameManager.Instance.isGameOver      = false;
        GameManager.Instance.isLevelCompleted = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopAllSoundsExceptMusic();
            AudioManager.Instance.PlaySfx("Success");
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.CalculateHeroPoints();

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

        // Show Survived panel and hide HUD — timeScale stays at 1 (no PauseGame) to
        // avoid the LoadScene freeze that happens when timeScale is 0 during scene load.
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetHUDActive(false);
            UIManager.Instance.ToggleLevelCompleteUI();
        }

        // Wait 2 seconds in real time then async-load the main menu
        StartCoroutine(AfterLevelComplete());
    }

    private IEnumerator AfterLevelComplete()
    {
        yield return new WaitForSecondsRealtime(2f);
        ReturnToMenu();
    }

    /// <summary>
    /// Destroys the current level instance and returns to the main menu scene.
    /// Uses async loading to avoid blocking the main thread (sync LoadScene froze the editor
    /// on large scenes like Main.unity).
    /// </summary>
    public void ReturnToMenu()
    {
        Debug.Log("[LevelManager] ReturnToMenu: Starting async return to menu.");
        StartCoroutine(ReturnToMenuAsync());
    }

    private IEnumerator ReturnToMenuAsync()
    {
        _currentLevelData = null;

        // Reset time and pause states BEFORE loading
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isPaused = false;
            GameManager.Instance.isLevelCompleted = false;
        }

        // Use async load — this keeps the engine alive and responsive during the scene load
        // We keep the current level and UI visible as a buffer until the Main menu is ready.
        Debug.Log($"[LevelManager] ReturnToMenu: Async loading Main Menu scene: {mainMenu}");
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(mainMenu);
        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Clean up references now that the scene has transitioned
        _currentLevelInstance = null;

        Debug.Log("[LevelManager] ReturnToMenu: Main Menu scene fully loaded.");
    }
}
