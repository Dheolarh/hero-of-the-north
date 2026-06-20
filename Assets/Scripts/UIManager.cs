using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    public GameObject pauseMenu;
    public GameObject gameOverUI;
    public GameObject levelCompleteUI;
    public GameObject leaderboardUI;
    public GameObject lockedLevelUI;
    public GameObject HUD;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            RegisterAllPanels();
        }
        else
        {
            Destroy(gameObject);
        }
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
        RegisterAllPanels();
        HidePanels();

        if (scene.name == "Main")
        {
            activeCoroutines.Clear();
            SetHUDActive(false);
            BindMainMenuButtons();
        }
    }

    void Start()
    {
        RegisterAllPanels();
        HidePanels();

        // Initial hide on first load
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Main")
        {
            SetHUDActive(false);
            BindMainMenuButtons();
        }
    }

    private void BindMainMenuButtons()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var btn in buttons)
        {
            if (btn == null) continue;

            // The leaderboard button in the Main menu scene is named "Leaderboard" 
            // and is not a child of the persistent UIManager
            if (btn.gameObject.name == "Leaderboard" && !btn.transform.IsChildOf(transform))
            {
                // Clear both Inspector and runtime listeners to prevent double-firing/double-toggles
                btn.onClick = new Button.ButtonClickedEvent();
                btn.onClick.AddListener(ToggleLeaderboardUI);
                btn.onClick.AddListener(PlayButtonClickSound);
                Debug.Log("[UIManager] Programmatically bound Leaderboard button click event.");
            }
        }
    }

    private void PlayButtonClickSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSound();
        }
    }

    private bool IsManagerGameObject(GameObject go)
    {
        if (go == null) return false;
        return go.GetComponent<LevelManager>() != null ||

               go.GetComponent<GameManager>() != null ||

               go.GetComponent<UIManager>() != null ||

               go.GetComponent<AudioManager>() != null;
    }

    public void RegisterAllPanels()
    {
        // Safeguard: Reset inspector/existing references if they point to Manager GameObjects (which would disable the managers on HidePanels)
        if (IsManagerGameObject(pauseMenu)) pauseMenu = null;
        if (IsManagerGameObject(gameOverUI)) gameOverUI = null;
        if (IsManagerGameObject(levelCompleteUI)) levelCompleteUI = null;
        if (IsManagerGameObject(leaderboardUI)) leaderboardUI = null;
        if (IsManagerGameObject(lockedLevelUI)) lockedLevelUI = null;

        UIPanel[] panels = FindObjectsByType<UIPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);


        foreach (var panel in panels)
        {
            if (panel == null) continue;

            // Safeguard: Never register GameObjects containing critical persistent managers as UI panels
            if (IsManagerGameObject(panel.gameObject))
            {
                Debug.LogWarning($"[UIManager] Safeguard: Prevented registering manager GameObject '{panel.gameObject.name}' as a UI panel.");
                continue;
            }

            // Safeguard: Skip panels belonging to duplicate UIManagers that are about to be destroyed
            UIManager parentUIManager = panel.GetComponentInParent<UIManager>();
            if (parentUIManager != null && parentUIManager != this)
            {
                continue;
            }

            LeaderboardUI lUI = panel.GetComponent<LeaderboardUI>();
            if (lUI != null)
            {
                // If there's already an active instance of LeaderboardUI, and it's not this one,
                // this one will destroy itself. Skip registering it as the main panel reference.
                if (LeaderboardUI.Instance != null && LeaderboardUI.Instance != lUI)
                {
                    continue;
                }
            }

            switch (panel.Type)
            {
                case UIPanel.PanelType.PauseMenu:
                    pauseMenu = panel.gameObject;
                    break;
                case UIPanel.PanelType.GameOverUI:
                    gameOverUI = panel.gameObject;
                    break;
                case UIPanel.PanelType.LevelCompleteUI:
                    levelCompleteUI = panel.gameObject;
                    break;
                case UIPanel.PanelType.LeaderboardUI:
                    leaderboardUI = panel.gameObject;
                    break;
                case UIPanel.PanelType.LockedLevelUI:
                    lockedLevelUI = panel.gameObject;
                    break;
            }
        }

        // Fallback for LockedLevelUI in case the component is on a child object
        if (lockedLevelUI == null)
        {
            LockedLevelCountdown lcd = FindFirstObjectByType<LockedLevelCountdown>(FindObjectsInactive.Include);
            if (lcd != null)
            {
                if (!IsManagerGameObject(lcd.gameObject))
                {
                    lockedLevelUI = lcd.gameObject;
                    Debug.Log($"[UIManager] Dynamic fallback: Found LockedLevelCountdown on GameObject '{lockedLevelUI.name}' and registered it as LockedLevelUI.");
                }
                else
                {
                    Debug.LogWarning($"[UIManager] Safeguard: Found LockedLevelCountdown on Manager GameObject '{lcd.gameObject.name}' - skipped assignment to prevent deactivating the manager! Please check where the LockedLevelCountdown script is attached in the scene.");
                }
            }
        }

        Debug.Log($"[UIManager] RegisterAllPanels complete: Pause={pauseMenu}, GameOver={gameOverUI}, LevelComplete={levelCompleteUI}, Leaderboard={leaderboardUI}, LockedLevel={lockedLevelUI}");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Only toggle pause if not in other menus
            bool goActive = gameOverUI != null && gameOverUI.activeSelf;
            bool lcActive = levelCompleteUI != null && levelCompleteUI.activeSelf;
            if (!goActive && !lcActive)
            {
                TogglePauseMenu();
            }
        }
    }

    private Dictionary<GameObject, Coroutine> activeCoroutines =
        new Dictionary<GameObject, Coroutine>();

    public void TogglePauseMenu()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx("ButtonClick");
        if (GameManager.Instance.isPaused)
        {
            Debug.Log("[UIManager] TogglePauseMenu: Resuming game...");
            GameManager.Instance.ResumeGame();
        }
        else
        {
            if (pauseMenu == null) return;
            Debug.Log("[UIManager] TogglePauseMenu: Pausing game...");
            GameManager.Instance.PauseGame();
            ShowPanel(pauseMenu);
            SetHUDActive(false);
        }
    }

    public void ToggleGameOverUI()
    {
        if (gameOverUI == null) return;
        TogglePanel(gameOverUI);
        if (gameOverUI.activeSelf)

        {
            Debug.Log("[UIManager] Game Over UI opened. Hiding HUD.");
            SetHUDActive(false);
        }
    }

    public void ToggleLevelCompleteUI()
    {
        if (levelCompleteUI == null) return;
        TogglePanel(levelCompleteUI);
        if (levelCompleteUI.activeSelf)

        {
            Debug.Log("[UIManager] Level Complete UI opened. Hiding HUD.");
            SetHUDActive(false);
        }
    }

    public void SetHUDActive(bool active)
    {
        if (HUD == null)

        {
            // Try to find HUD component dynamically in the scene (including inactive GameObjects)
            HUD HUDComponent = FindFirstObjectByType<HUD>(FindObjectsInactive.Include);
            if (HUDComponent != null)
            {
                HUD = HUDComponent.gameObject;
            }
        }

        if (HUD == null)
        {
            Debug.LogError("[UIManager] HUD reference is NULL!");
            return;
        }


        Debug.Log($"[UIManager] SetHUDActive called. Active: {active}");

        if (activeCoroutines.ContainsKey(HUD))
        {
            StopCoroutine(activeCoroutines[HUD]);
            activeCoroutines.Remove(HUD);
        }

        HUD.transform.localScale = Vector3.one;
        HUD.SetActive(active);
    }

    public void ToggleLeaderboardUI() { if (leaderboardUI != null) TogglePanel(leaderboardUI); }
    public void ToggleLockedLevelUI() { if (lockedLevelUI != null) TogglePanel(lockedLevelUI); }

    public void ClosePauseMenu()
    {
        if (pauseMenu != null && pauseMenu.activeSelf)
        {
            HidePanel(pauseMenu);
            SetHUDActive(true);
        }
    }

    private void TogglePanel(GameObject panel)
    {
        if (panel == null) return;

        bool isActive = panel.activeSelf;
        if (isActive)
        {
            HidePanel(panel);
        }
        else
        {
            ShowPanel(panel);
        }
    }

    private void ShowPanel(GameObject panel)
    {
        if (activeCoroutines.ContainsKey(panel))
        {
            StopCoroutine(activeCoroutines[panel]);
            activeCoroutines.Remove(panel);
        }

        // Reset all target scales to zero before activating so AnimateShow
        // always starts from a clean state (prevents panels stuck invisible
        // if a previous AnimateHide left children at scale zero)
        bool hasCanvas = panel.GetComponent<Canvas>() != null;
        if (hasCanvas)
        {
            foreach (Transform child in panel.transform)
                if (child != null) child.localScale = Vector3.zero;
        }
        else
        {
            panel.transform.localScale = Vector3.zero;
        }

        panel.SetActive(true);
        activeCoroutines[panel] = StartCoroutine(AnimateShow(panel));
    }

    private void HidePanel(GameObject panel)
    {
        if (activeCoroutines.ContainsKey(panel))
        {
            StopCoroutine(activeCoroutines[panel]);
            activeCoroutines.Remove(panel);
        }

        activeCoroutines[panel] = StartCoroutine(AnimateHide(panel));
    }

    private IEnumerator AnimateShow(GameObject panel)
    {
        // Collect targets: if Canvas, get all immediate children. If not, get self.
        System.Collections.Generic.List<Transform> targets = new System.Collections.Generic.List<Transform>();

        if (panel.GetComponent<Canvas>() != null)
        {
            foreach (Transform child in panel.transform)
            {
                targets.Add(child);
            }
        }
        else
        {
            targets.Add(panel.transform);
        }

        // Set initial scale
        foreach (var t in targets)
        {
            if (t != null) t.localScale = Vector3.zero;
        }

        float duration = 0.3f;
        float elapsed = 0f;

        // Scale up with overshoot (bounce)
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;

            float scale;
            if (progress < 0.8f)
            {
                // 0 to 1.1
                float subProgress = progress / 0.8f;
                scale = Mathf.Lerp(0f, 1.1f, Mathf.SmoothStep(0f, 1f, subProgress));
            }
            else
            {
                // 1.1 to 1.0
                float subProgress = (progress - 0.8f) / 0.2f;
                scale = Mathf.Lerp(1.1f, 1.0f, subProgress);
            }

            foreach (var t in targets)
            {
                if (t != null) t.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        foreach (var t in targets)
        {
            if (t != null) t.localScale = Vector3.one;
        }

        activeCoroutines.Remove(panel);
    }

    private IEnumerator AnimateHide(GameObject panel)
    {
        System.Collections.Generic.List<Transform> targets = new System.Collections.Generic.List<Transform>();

        if (panel.GetComponent<Canvas>() != null)
        {
            foreach (Transform child in panel.transform)
            {
                targets.Add(child);
            }
        }
        else
        {
            targets.Add(panel.transform);
        }

        // Store initial scales? Assuming they are 1 is safer for consistent hide.
        // Or read from first target?
        Vector3 initialScale = Vector3.one;
        if (targets.Count > 0 && targets[0] != null) initialScale = targets[0].localScale;

        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;

            // Smooth step down
            float scale = Mathf.Lerp(initialScale.x, 0f, Mathf.SmoothStep(0f, 1f, progress));

            foreach (var t in targets)
            {
                if (t != null) t.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        foreach (var t in targets)
        {
            if (t != null) t.localScale = Vector3.zero;
        }

        panel.SetActive(false);
        activeCoroutines.Remove(panel);
    }

    public void HidePanels()
    {
        if (pauseMenu != null && pauseMenu.activeSelf) pauseMenu.SetActive(false);
        if (gameOverUI != null && gameOverUI.activeSelf) gameOverUI.SetActive(false);
        if (levelCompleteUI != null && levelCompleteUI.activeSelf) levelCompleteUI.SetActive(false);
        if (leaderboardUI != null && leaderboardUI.activeSelf) leaderboardUI.SetActive(false);
        if (lockedLevelUI != null && lockedLevelUI.activeSelf) lockedLevelUI.SetActive(false);
    }
}
