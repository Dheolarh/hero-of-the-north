using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level Configuration")]
    [SerializeField] private List<LevelData> allLevels = new List<LevelData>();

    [System.Serializable]
    public struct CommunityPrefabMapping
    {
        public string toolName;
        public GameObject prefab;
    }

    [Header("Community Level Builders")]
    [Tooltip("Prefabs mapping list used to dynamically build community levels inside the Game scene.")]
    [SerializeField] private List<CommunityPrefabMapping> communityPrefabs = new List<CommunityPrefabMapping>();

    [Header("Community Level Environment")]
    [Tooltip("Prefab for the default background sky/scene to instantiate on community levels.")]
    [SerializeField] private GameObject defaultBackgroundPrefab;
    [Tooltip("Prefab for the default boundary barrier boundaries/walls to instantiate on community levels.")]
    [SerializeField] private GameObject defaultBarrierPrefab;
    [Tooltip("Prefab for the default background clouds to instantiate on community levels.")]
    [SerializeField] private GameObject defaultCloudsPrefab;

    [Header("Scene Names")]
    public string mainMenu = "Main";
    [Tooltip("The dedicated empty scene for gameplay. Keep game UI here, separate from Main menu UI.")]
    public string gameScene = "Game";

    [Header("Testing & Debugging")]
    [Tooltip("If true, all levels with prefabs assigned will be treated as unlocked, bypassing server lock conditions.")]
    public bool bypassLevelLock = false;

    // ── Runtime state ──────────────────────────────────────────────────────
    private int          _currentLevelIndex = 0;
    private int          _highestUnlockedLevel = 1;
    private LevelData    _currentLevelData;
    private GameObject   _currentLevelInstance;
    private string       _queuedCommunityLevelJson = "";

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

        // Check if loading a community level from playerprefs
        if (PlayerPrefs.HasKey("PlayCommunityLevelJSON"))
        {
            string json = PlayerPrefs.GetString("PlayCommunityLevelJSON", "");
            PlayerPrefs.DeleteKey("PlayCommunityLevelJSON");
            PlayerPrefs.Save();
            PlayCommunityLevel(json);
        }
    }

    /// <summary>
    /// When the Game scene finishes loading, automatically spawn the queued level prefab.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[LevelManager] OnSceneLoaded: Loaded scene '{scene.name}'");
        if (scene.name == gameScene)
        {
            if (!string.IsNullOrEmpty(_queuedCommunityLevelJson))
            {
                SpawnCommunityLevel();
            }
            else if (_currentLevelData != null)
            {
                SpawnLevel();
            }
            else
            {
                Debug.LogWarning("[LevelManager] OnSceneLoaded: No level queued to spawn!");
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

        if (bypassLevelLock)
            return true;

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
        // Always reset time scale — if the player retried from the pause menu,
        // timeScale is still 0 and the game would appear frozen after restart
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isPaused        = false;
            GameManager.Instance.isGameOver      = false;
            GameManager.Instance.isLevelCompleted = false;
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.StopAllSoundsExceptMusic();

        if (UIManager.Instance != null)
            UIManager.Instance.HidePanels();

        // If we are playtesting in the Level Creator scene, restart the playtest dynamically
        if (LevelCreatorUI.Instance != null && LevelCreatorUI.Instance.IsPlaytesting)
        {
            // Toggle playtest off and on to clear and rebuild the level instantly
            LevelCreatorUI.Instance.TogglePlaytest(); // turn off
            LevelCreatorUI.Instance.TogglePlaytest(); // turn on
            
            // Re-hide HUD in playtest mode
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetHUDActive(false);
            }
            return;
        }

        // If we are playing a community level in the Game scene, rebuild it instantly to restart
        if (!string.IsNullOrEmpty(_queuedCommunityLevelJson))
        {
            SpawnCommunityLevel();
            return;
        }

        if (_currentLevelData == null)
        {
            Debug.LogWarning("[LevelManager] RestartLevel called but no level is loaded!");
            return;
        }

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.IncrementRetryCount();

        SpawnLevel(); // Re-instantiate prefab in place
    }

    public void PlayCommunityLevel(string json)
    {
        _queuedCommunityLevelJson = json;
        _currentLevelData = null; // custom level
        
        // Reset state
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isGameOver = false;
            GameManager.Instance.isLevelCompleted = false;
        }

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.ResetForNewLevel(-1); // community level index

        if (SceneManager.GetActiveScene().name == gameScene)
            SpawnCommunityLevel();
        else
            SceneManager.LoadScene(gameScene);
    }

    private GameObject GetCommunityPrefab(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        string norm = name.Replace(" ", "").ToLower();
        foreach (var mapping in communityPrefabs)
        {
            if (!string.IsNullOrEmpty(mapping.toolName) && mapping.toolName.Replace(" ", "").ToLower() == norm)
            {
                return mapping.prefab;
            }
        }
        return null;
    }

    private void SpawnCommunityLevel()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopAllSoundsExceptMusic();

        // Destroy previous level instance
        if (_currentLevelInstance != null)
        {
            Destroy(_currentLevelInstance);
            _currentLevelInstance = null;
        }

        if (string.IsNullOrEmpty(_queuedCommunityLevelJson))
        {
            Debug.LogError("[LevelManager] No custom community level JSON queued!");
            return;
        }

        CustomLevelData data = JsonUtility.FromJson<CustomLevelData>(_queuedCommunityLevelJson);
        if (data == null)
        {
            Debug.LogError("[LevelManager] Failed to parse custom level JSON!");
            return;
        }

        // Create a root object for the spawned level
        _currentLevelInstance = new GameObject("CommunityLevel_Spawned");

        // Create dedicated GameObject for LevelCameraSettings to match campaign hierarchy
        GameObject camSettingsObj = new GameObject("LevelCameraSettings");
        camSettingsObj.transform.SetParent(_currentLevelInstance.transform);
        LevelCameraSettings camSettings = camSettingsObj.AddComponent<LevelCameraSettings>();
        camSettings.offset = new Vector3(data.camOffsetX, data.camOffsetY, -10f);
        camSettings.orthoSize = data.camOrthoSize;
        camSettings.followX = true;
        camSettings.followY = true;
        camSettings.useSmoothing = true;
        camSettings.smoothSpeed = 5f;

        // Spawn default background environment if configured
        if (defaultBackgroundPrefab != null)
        {
            GameObject bg = Instantiate(defaultBackgroundPrefab, _currentLevelInstance.transform);
            bg.name = "Background";
            bg.transform.localPosition = new Vector3(22f, 18.5f, 0f);
            bg.transform.localScale = new Vector3(10f, 10f, 1f);
        }

        // Spawn default boundaries/walls if configured
        if (defaultBarrierPrefab != null)
        {
            GameObject bar = Instantiate(defaultBarrierPrefab, Vector3.zero, Quaternion.identity, _currentLevelInstance.transform);
            bar.name = "Barrier";
        }

        // Spawn default clouds if configured
        if (defaultCloudsPrefab != null)
        {
            GameObject clouds = Instantiate(defaultCloudsPrefab, _currentLevelInstance.transform);
            clouds.name = "Clouds";
            clouds.transform.localPosition = new Vector3(0f, 0f, 0f);
            clouds.transform.localScale = new Vector3(1f, 1f, 1f);
        }

        // 1. Spawn Player
        if (data.hasPlayerStart)
        {
            GameObject playerPrefab = GetCommunityPrefab("Hero") ?? GetCommunityPrefab("PlayerStart") ?? GetCommunityPrefab("Player");
            if (playerPrefab != null)
            {
                GameObject playerObj = Instantiate(playerPrefab, data.playerStartPos.ToVector2(), Quaternion.identity, _currentLevelInstance.transform);
                playerObj.name = "Player";
                
                // Set custom stats
                PlayerController pc = playerObj.GetComponent<PlayerController>() ?? playerObj.GetComponentInChildren<PlayerController>();
                if (pc != null)
                {
                    pc.Speed = data.playerMoveSpeed;
                    pc.JumpForce = data.playerJumpForce;
                    pc.MaxMultiJumps = data.playerMaxJumps;
                    pc.EnableFallDamage = data.playerEnableFallDamage;
                }

                // Wire Camera
                CameraFollow cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : FindFirstObjectByType<CameraFollow>();
                if (cam != null)
                {
                    Transform cameraTarget = pc != null ? pc.transform : playerObj.transform;
                    Debug.Log($"[LevelManager] Successfully found CameraFollow. Setting target to player: {cameraTarget.name}");
                    cam.SetTarget(cameraTarget);
                    cam.StartFollowing();
                    cam.InstantSnap();
                }
                else
                {
                    Debug.LogError("[LevelManager] CameraFollow component not found in scene!");
                }
            }
        }

        // 2. Spawn Goal Portal
        if (data.hasGoal)
        {
            GameObject goalPrefab = GetCommunityPrefab("Goal") ?? GetCommunityPrefab("Portal");
            if (goalPrefab != null)
            {
                GameObject goalObj = Instantiate(goalPrefab, data.goalPos.ToVector2(), Quaternion.identity, _currentLevelInstance.transform);
                goalObj.name = "GoalPortal";
                
                // Add LevelGoal component if not present
                if (goalObj.GetComponent<LevelGoal>() == null && goalObj.GetComponentInChildren<LevelGoal>() == null)
                {
                    Collider2D[] cols = goalObj.GetComponentsInChildren<Collider2D>(true);
                    GameObject target = cols.Length > 0 ? cols[0].gameObject : goalObj;
                    if (target.GetComponent<LevelGoal>() == null)
                    {
                        target.AddComponent<LevelGoal>();
                    }
                }
            }
        }

        // Keep track of spawned traps to copy links/wiring afterwards
        Dictionary<Vector2, GameObject> spawnedTraps = new Dictionary<Vector2, GameObject>();
        Dictionary<Vector2, CustomTrapData> trapDataMap = new Dictionary<Vector2, CustomTrapData>();

        // 3. Spawn Tiles
        foreach (var tile in data.tiles)
        {
            GameObject tilePrefab = GetCommunityPrefab(tile.type);
            if (tilePrefab != null)
            {
                Vector2 pos = tile.position.ToVector2();
                GameObject tileObj = Instantiate(tilePrefab, pos, Quaternion.Euler(0f, 0f, tile.rotation), _currentLevelInstance.transform);
                tileObj.transform.localScale = tile.scale.ToVector2();
            }
        }

        // 4. Spawn Traps
        foreach (var trap in data.traps)
        {
            GameObject trapPrefab = GetCommunityPrefab(trap.type);
            if (trapPrefab != null)
            {
                Vector2 pos = trap.spawnPos.ToVector2();
                GameObject trapObj = Instantiate(trapPrefab, pos, Quaternion.Euler(0f, 0f, trap.rotation), _currentLevelInstance.transform);
                trapObj.transform.localScale = trap.scale.ToVector2();

                spawnedTraps[pos] = trapObj;
                trapDataMap[pos] = trap;
            }
        }

        // 5. Wire connections (Triggers to Targets)
        foreach (var pair in spawnedTraps)
        {
            Vector2 pos = pair.Key;
            GameObject trapObj = pair.Value;
            CustomTrapData trapData = trapDataMap[pos];

            CollisionsAndTriggers ct = trapObj.GetComponent<CollisionsAndTriggers>() ?? trapObj.GetComponentInChildren<CollisionsAndTriggers>();
            if (ct == null && !string.IsNullOrEmpty(trapData.triggerTypeStr))
            {
                ct = trapObj.AddComponent<CollisionsAndTriggers>();
            }

            if (ct != null)
            {
                // Restore basic properties
                ct.activateOnStart = trapData.activateOnStart;
                ct.enableMove = trapData.enableMove;
                ct.moveSpeed = trapData.moveSpeed;
                ct.isPingPong = trapData.isPingPong;
                ct.pingPongDistance = trapData.pingPongDistance;
                ct.enableRotation = trapData.enableRotation;
                ct.rotationSpeed = trapData.rotationSpeed;
                ct.useLocalCoordinates = trapData.useLocalCoordinates;
                ct.targetPosition = trapData.targetPosition.ToVector2();
                ct.targetMoveSpeed = trapData.targetMoveSpeed;
                ct.teleportPosition = trapData.teleportPosition.ToVector2();
                ct.moveOnXOnly = trapData.moveOnXOnly;
                ct.moveOnYOnly = trapData.moveOnYOnly;
                ct.playAudioOnTrigger = trapData.playAudioOnTrigger;
                ct.audioClipName = trapData.audioClipName;
                ct.loopAudio = trapData.loopAudio;

                // Restore enums from strings
                if (!string.IsNullOrEmpty(trapData.triggerTypeStr))
                    System.Enum.TryParse(trapData.triggerTypeStr, out ct.triggerType);
                if (!string.IsNullOrEmpty(trapData.componentActionStr))
                    System.Enum.TryParse(trapData.componentActionStr, out ct.componentAction);
                if (!string.IsNullOrEmpty(trapData.activationModeStr))
                    System.Enum.TryParse(trapData.activationModeStr, out ct.activationMode);
                if (!string.IsNullOrEmpty(trapData.moveDirectionStr))
                    System.Enum.TryParse(trapData.moveDirectionStr, out ct.moveDirection);
                if (!string.IsNullOrEmpty(trapData.rotationDirectionStr))
                    System.Enum.TryParse(trapData.rotationDirectionStr, out ct.rotationDirection);

                // Restore Object properties
                ct.modifyColliderState = trapData.modifyColliderState;
                ct.makeSolid = trapData.makeSolid;
                ct.modifyGravityState = trapData.modifyGravityState;
                ct.makeSubjectToGravity = trapData.makeSubjectToGravity;
                ct.appearOnTrigger = trapData.appearOnTrigger;
                ct.deleteTriggerZone = trapData.deleteTriggerZone;

                // Restore Camera Shake settings
                ct.enableCameraShake = trapData.enableCameraShake;
                ct.playShakeSFX = trapData.playShakeSFX;
                ct.cameraShakeIntensity = trapData.cameraShakeIntensity;
                ct.cameraShakeFrequency = trapData.cameraShakeFrequency;
                ct.stopShakeOnExitBoundary = trapData.stopShakeOnExitBoundary;

                // Wire target objects
                if (trapData.hasTarget)
                {
                    Vector2 targetPos = trapData.targetPos.ToVector2();
                    if (spawnedTraps.ContainsKey(targetPos))
                    {
                        ct.objectToModify = spawnedTraps[targetPos];
                    }
                }

                // Wire destination target object
                Vector2 destPos = trapData.destinationTargetPos.ToVector2();
                if (spawnedTraps.ContainsKey(destPos))
                {
                    ct.destinationTargetObject = spawnedTraps[destPos];
                }

                // Wire multiple objects
                if (trapData.objectsToTriggerPositions != null && trapData.objectsToTriggerPositions.Count > 0)
                {
                    List<GameObject> targetList = new List<GameObject>();
                    foreach (var objPos in trapData.objectsToTriggerPositions)
                    {
                        Vector2 oPos = objPos.ToVector2();
                        if (spawnedTraps.ContainsKey(oPos))
                        {
                            targetList.Add(spawnedTraps[oPos]);
                        }
                    }
                    ct.objectsToTrigger = targetList.ToArray();
                }

                // Wire activation objects
                if (trapData.activationObjectsPositions != null && trapData.activationObjectsPositions.Count > 0)
                {
                    List<GameObject> activatorList = new List<GameObject>();
                    foreach (var objPos in trapData.activationObjectsPositions)
                    {
                        Vector2 oPos = objPos.ToVector2();
                        if (spawnedTraps.ContainsKey(oPos))
                        {
                            activatorList.Add(spawnedTraps[oPos]);
                        }
                    }
                    ct.activationObjects = activatorList.ToArray();
                }
            }
        }

        // Force Main Camera viewport update
        var mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.orthographicSize = data.camOrthoSize;
        }

        // Enable HUD and Controls in game scene
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetHUDActive(true);
            UIManager.Instance.SetDirectionControlsActive(true);
        }
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
        if (GameManager.Instance.isLevelCompleted || GameManager.Instance.isGameOver) return;

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
