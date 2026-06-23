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
    public GameObject editModeUI;
    public GameObject tutorialPanel;
    public GameObject messagePanel;
    public GameObject HUD;

    [Header("Tutorial Settings")]
    public string scrollOpenSoundName = "ScrollOpen";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ResolveInspectorReferences();
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
        ResolveInspectorReferences();
        RegisterAllPanels();
        HidePanels();

        if (scene.name == "Main")
        {
            activeCoroutines.Clear();
            SetHUDActive(false);
            BindMainMenuButtons();
            CheckFirstTimeLaunch();
        }
    }

    void Start()
    {
        ResolveInspectorReferences();
        RegisterAllPanels();
        HidePanels();

        // Initial hide on first load
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Main")
        {
            SetHUDActive(false);
            BindMainMenuButtons();
            CheckFirstTimeLaunch();
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

            // Bind Scroll / Story / Tutorial button
            if ((btn.gameObject.name == "Scroll" || btn.gameObject.name == "ScrollButton" || btn.gameObject.name == "Story" || btn.gameObject.name == "StoryButton") && !btn.transform.IsChildOf(transform))
            {
                btn.onClick = new Button.ButtonClickedEvent();
                btn.onClick.AddListener(ToggleTutorialPanel);
                btn.onClick.AddListener(PlayButtonClickSound);
                Debug.Log($"[UIManager] Programmatically bound Scroll/Story button: {btn.gameObject.name}");
            }

            // Bind Message button
            if ((btn.gameObject.name == "MessageButton" || btn.gameObject.name == "MessageBtn" || btn.gameObject.name == "ShowMessage") && !btn.transform.IsChildOf(transform))
            {
                btn.onClick = new Button.ButtonClickedEvent();
                btn.onClick.AddListener(ToggleMessagePanel);
                btn.onClick.AddListener(PlayButtonClickSound);
                Debug.Log($"[UIManager] Programmatically bound Message button: {btn.gameObject.name}");
            }
        }

        BindCloseButtonForPanel(tutorialPanel, CloseTutorialPanel);
        BindCloseButtonForPanel(leaderboardUI, CloseLeaderboardUI);
        BindCloseButtonForPanel(messagePanel, CloseMessagePanel);
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
        if (IsManagerGameObject(editModeUI)) editModeUI = null;
        if (IsManagerGameObject(tutorialPanel)) tutorialPanel = null;
        if (IsManagerGameObject(messagePanel)) messagePanel = null;

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

            GameObject targetPanelGo = ResolvePanelGameObject(panel.gameObject);

            switch (panel.Type)
            {
                case UIPanel.PanelType.PauseMenu:
                    pauseMenu = targetPanelGo;
                    break;
                case UIPanel.PanelType.GameOverUI:
                    gameOverUI = targetPanelGo;
                    break;
                case UIPanel.PanelType.LevelCompleteUI:
                    levelCompleteUI = targetPanelGo;
                    break;
                case UIPanel.PanelType.LeaderboardUI:
                    leaderboardUI = targetPanelGo;
                    break;
                case UIPanel.PanelType.TutorialPanel:
                    tutorialPanel = targetPanelGo;
                    break;
                case UIPanel.PanelType.MessagePanel:
                    messagePanel = targetPanelGo;
                    break;
            }
        }

        Debug.Log($"[UIManager] RegisterAllPanels complete: Pause={pauseMenu}, GameOver={gameOverUI}, LevelComplete={levelCompleteUI}, Leaderboard={leaderboardUI}, Tutorial={tutorialPanel}, Message={messagePanel}");
    }

    private GameObject ResolvePanelGameObject(GameObject go)
    {
        if (go == null) return null;

        // Clean up legacy DisableOnClick component on the Canvas/object itself if present
        var disableOnClick = go.GetComponent<DisableOnClick>();
        if (disableOnClick != null)
        {
            Debug.LogWarning($"[UIManager] Found legacy DisableOnClick on Canvas/object '{go.name}' - destroying it to prevent accidental deactivation.");
            Destroy(disableOnClick);
        }

        Canvas canvas = go.GetComponent<Canvas>();
        if (canvas != null)
        {
            Transform panelChild = go.transform.Find("Panel");
            if (panelChild == null)
            {
                panelChild = go.transform.Find("Scroll");
            }
            if (panelChild == null)
            {
                UIPanel uiPanelChild = go.GetComponentInChildren<UIPanel>(true);
                if (uiPanelChild != null && uiPanelChild.gameObject != go)
                {
                    panelChild = uiPanelChild.transform;
                }
            }
            if (panelChild == null && go.transform.childCount > 0)
            {
                panelChild = go.transform.GetChild(0);
            }

            if (panelChild != null)
            {
                var childDisableOnClick = panelChild.GetComponent<DisableOnClick>();
                if (childDisableOnClick != null)
                {
                    Debug.LogWarning($"[UIManager] Found legacy DisableOnClick on child panel '{panelChild.name}' - destroying it to prevent accidental deactivation.");
                    Destroy(childDisableOnClick);
                }

                Debug.Log($"[UIManager] Resolved Canvas GameObject '{go.name}' to its child panel '{panelChild.name}' to avoid Canvas toggles/attachment.");
                return panelChild.gameObject;
            }
        }
        return go;
    }

    private void ResolveInspectorReferences()
    {
        pauseMenu = ResolvePanelGameObject(pauseMenu);
        gameOverUI = ResolvePanelGameObject(gameOverUI);
        levelCompleteUI = ResolvePanelGameObject(levelCompleteUI);
        leaderboardUI = ResolvePanelGameObject(leaderboardUI);
        editModeUI = ResolvePanelGameObject(editModeUI);
        tutorialPanel = ResolvePanelGameObject(tutorialPanel);
        messagePanel = ResolvePanelGameObject(messagePanel);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Only toggle pause if not in other menus
            bool goActive = gameOverUI != null && gameOverUI.activeInHierarchy;
            bool lcActive = levelCompleteUI != null && levelCompleteUI.activeInHierarchy;
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
        if (gameOverUI.activeInHierarchy)
        {
            Debug.Log("[UIManager] Game Over UI opened. Hiding HUD.");
            SetHUDActive(false);
        }
    }

    public void ToggleLevelCompleteUI()
    {
        if (levelCompleteUI == null) return;
        TogglePanel(levelCompleteUI);
        if (levelCompleteUI.activeInHierarchy)
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

    public void ToggleLeaderboardUI()
    {
        if (leaderboardUI != null)
        {
            if (!leaderboardUI.activeInHierarchy)
                OpenLeaderboardUI();
        }
    }

    public void OpenLeaderboardUI()
    {
        OpenUnrollingPanel(leaderboardUI, scrollOpenSoundName);
    }

    public void CloseLeaderboardUI()
    {
        CloseUnrollingPanel(leaderboardUI);
    }

    public void ToggleTutorialPanel()
    {
        if (tutorialPanel != null)
        {
            if (!tutorialPanel.activeInHierarchy)
                OpenTutorialPanel();
        }
    }

    public void OpenTutorialPanel()
    {
        OpenUnrollingPanel(tutorialPanel, scrollOpenSoundName);
    }

    public void CloseTutorialPanel()
    {
        CloseUnrollingPanel(tutorialPanel);
    }

    public void ToggleMessagePanel()
    {
        if (messagePanel != null)
        {
            if (!messagePanel.activeInHierarchy)
                OpenMessagePanel();
        }
    }

    public void OpenMessagePanel()
    {
        OpenUnrollingPanel(messagePanel, scrollOpenSoundName);
    }

    public void CloseMessagePanel()
    {
        CloseUnrollingPanel(messagePanel);
    }

    public void OpenUnrollingPanel(GameObject panel, string openSoundName = "ScrollOpen")
    {
        if (panel == null) return;

        // Play unroll sound effect
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(openSoundName))
        {
            AudioManager.Instance.PlaySfx(openSoundName);
        }

        // Ensure the parent Canvas is active
        Canvas parentCanvas = panel.GetComponentInParent<Canvas>(true);
        if (parentCanvas != null)
        {
            parentCanvas.gameObject.SetActive(true);
        }

        // Find the "Scroll" child object inside this panel
        RectTransform scrollRect = GetScrollObjectFromPanel(panel);

        if (activeCoroutines.ContainsKey(panel))
        {
            StopCoroutine(activeCoroutines[panel]);
            activeCoroutines.Remove(panel);
        }

        // Activate the panel
        panel.SetActive(true);

        // Reset any nested ScrollRect positions to the top
        ScrollRect[] scrollRects = panel.GetComponentsInChildren<ScrollRect>(true);
        foreach (var sRect in scrollRects)
        {
            if (sRect != null)
            {
                Canvas.ForceUpdateCanvases();
                sRect.verticalNormalizedPosition = 1f;
                sRect.horizontalNormalizedPosition = 0f;
            }
        }

        if (scrollRect != null)
        {
            // Ensure the parent panel BG is active and at full scale
            Transform panelBG = panel.transform.Find("Panel");
            if (panelBG != null)
            {
                panelBG.gameObject.SetActive(true);
                panelBG.localScale = Vector3.one;
            }

            activeCoroutines[panel] = StartCoroutine(AnimateScrollUnroll(scrollRect, panel));
        }
        else
        {
            // Fallback to standard show animation
            activeCoroutines[panel] = StartCoroutine(AnimateShow(panel));
        }
    }

    public void CloseUnrollingPanel(GameObject panel)
    {
        if (panel == null) return;

        // Play scroll sound effect on close as well
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(scrollOpenSoundName))
        {
            AudioManager.Instance.PlaySfx(scrollOpenSoundName);
        }

        // Find the "Scroll" child object inside this panel
        RectTransform scrollRect = GetScrollObjectFromPanel(panel);

        if (activeCoroutines.ContainsKey(panel))
        {
            StopCoroutine(activeCoroutines[panel]);
            activeCoroutines.Remove(panel);
        }

        if (scrollRect != null)
        {
            activeCoroutines[panel] = StartCoroutine(AnimateScrollRollUp(scrollRect, panel));
        }
        else
        {
            // Fallback to standard hide animation
            activeCoroutines[panel] = StartCoroutine(AnimateHide(panel));
        }
    }

    private RectTransform GetScrollObjectFromPanel(GameObject panel)
    {
        if (panel == null) return null;

        // First try to find directly under Panel/Scroll
        Transform scrollTransform = panel.transform.Find("Panel/Scroll");
        if (scrollTransform == null)
        {
            // Search all children for name "Scroll", "ScrollArea", or "ScrollPanel"
            foreach (Transform child in panel.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Scroll" || child.name == "ScrollArea" || child.name == "ScrollPanel")
                {
                    scrollTransform = child;
                    break;
                }
            }
        }

        // Fallback to "Panel" if Scroll is not found
        if (scrollTransform == null)
        {
            scrollTransform = panel.transform.Find("Panel");
        }

        return scrollTransform != null ? scrollTransform.GetComponent<RectTransform>() : panel.GetComponent<RectTransform>();
    }

    private IEnumerator AnimateScrollUnroll(RectTransform scrollRect, GameObject panel)
    {
        // Ensure there is a RectMask2D component on the Scroll object to mask the children content
        RectMask2D mask = scrollRect.gameObject.GetComponent<RectMask2D>();
        if (mask == null)
        {
            mask = scrollRect.gameObject.AddComponent<RectMask2D>();
        }

        // Enable the mask for the duration of the unrolling animation
        mask.enabled = true;

        // Force a layout rebuild to get the true laid-out height of the panel
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect);
        float targetHeight = scrollRect.rect.height;
        if (targetHeight <= 0f) targetHeight = 800f; // fallback

        // Animate bottom padding from targetHeight to 0 to unroll top-to-bottom
        float duration = 0.5f; // Smooth reveal duration
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            
            // Smooth unrolling curve (ease out sine)
            float t = Mathf.Sin(progress * Mathf.PI * 0.5f);
            float currentPaddingBottom = Mathf.Lerp(targetHeight, 0f, t);
            
            mask.padding = new Vector4(0f, currentPaddingBottom, 0f, 0f);
            yield return null;
        }

        mask.padding = Vector4.zero;
        
        // Disable the mask after animation completes so it doesn't clip nested child UI elements (like scroll entries) at runtime
        mask.enabled = false;

        activeCoroutines.Remove(panel);
    }

    private IEnumerator AnimateScrollRollUp(RectTransform scrollRect, GameObject panel)
    {
        RectMask2D mask = scrollRect.gameObject.GetComponent<RectMask2D>();
        if (mask == null)
        {
            mask = scrollRect.gameObject.AddComponent<RectMask2D>();
        }

        // Enable the mask for the rollup animation
        mask.enabled = true;

        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect);
        float targetHeight = scrollRect.rect.height;
        if (targetHeight <= 0f) targetHeight = 800f; // fallback

        // Animate bottom padding from 0 to targetHeight to roll up bottom-to-top
        float duration = 0.3f; // Smooth roll up duration
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            
            // Ease in quad
            float t = progress * progress;
            float currentPaddingBottom = Mathf.Lerp(0f, targetHeight, t);
            
            mask.padding = new Vector4(0f, currentPaddingBottom, 0f, 0f);
            yield return null;
        }

        mask.padding = new Vector4(0f, targetHeight, 0f, 0f);
        
        // Reset padding and disable mask before disabling panel
        mask.padding = Vector4.zero;
        mask.enabled = false;
        
        panel.SetActive(false);
        activeCoroutines.Remove(panel);
    }

    private void CheckFirstTimeLaunch()
    {
        // Track first time launch using PlayerPrefs
        if (!PlayerPrefs.HasKey("HasLaunchedBefore"))
        {
            PlayerPrefs.SetInt("HasLaunchedBefore", 1);
            PlayerPrefs.Save();

            // Open the tutorial panel automatically on first launch
            StartCoroutine(ShowTutorialOnFirstLaunchDelay());
        }
    }

    private IEnumerator ShowTutorialOnFirstLaunchDelay()
    {
        // Wait a small moment to let the scene fully settle, then show the tutorial panel
        yield return new WaitForSeconds(0.5f);
        OpenTutorialPanel();
    }

    private void BindCloseButtonForPanel(GameObject panel, UnityEngine.Events.UnityAction closeAction)
    {
        if (panel == null) return;

        // Search for buttons inside the panel (including inactive ones)
        Button[] buttons = panel.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            string nameLower = btn.gameObject.name.ToLower();
            if (nameLower == "close" || nameLower == "closebutton" || nameLower == "close_btn" || nameLower == "x" || nameLower == "closetutorial" || nameLower == "closeleaderboard" || nameLower == "closemessage")
            {
                btn.onClick = new Button.ButtonClickedEvent();
                btn.onClick.AddListener(closeAction);
                Debug.Log($"[UIManager] Programmatically bound Close button for panel '{panel.name}': {btn.gameObject.name}");
            }
        }
    }

    public void ClosePauseMenu()
    {
        if (pauseMenu != null && pauseMenu.activeInHierarchy)
        {
            HidePanel(pauseMenu);
            SetHUDActive(true);
        }
    }

    private void TogglePanel(GameObject panel)
    {
        if (panel == null) return;

        bool isActive = panel.activeInHierarchy;
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
        if (panel == null) return;

        if (activeCoroutines.ContainsKey(panel))
        {
            StopCoroutine(activeCoroutines[panel]);
            activeCoroutines.Remove(panel);
        }

        // Ensure the parent Canvas is active
        Canvas parentCanvas = panel.GetComponentInParent<Canvas>(true);
        if (parentCanvas != null)
        {
            parentCanvas.gameObject.SetActive(true);
        }

        panel.transform.localScale = Vector3.zero;
        panel.SetActive(true);

        // Reset any nested ScrollRect positions to the top
        ScrollRect[] scrollRects = panel.GetComponentsInChildren<ScrollRect>(true);
        foreach (var sRect in scrollRects)
        {
            if (sRect != null)
            {
                Canvas.ForceUpdateCanvases();
                sRect.verticalNormalizedPosition = 1f;
                sRect.horizontalNormalizedPosition = 0f;
            }
        }

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
        panel.transform.localScale = Vector3.zero;

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
                float subProgress = progress / 0.8f;
                scale = Mathf.Lerp(0f, 1.1f, Mathf.SmoothStep(0f, 1f, subProgress));
            }
            else
            {
                float subProgress = (progress - 0.8f) / 0.2f;
                scale = Mathf.Lerp(1.1f, 1.0f, subProgress);
            }

            panel.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        panel.transform.localScale = Vector3.one;
        activeCoroutines.Remove(panel);
    }

    private IEnumerator AnimateHide(GameObject panel)
    {
        Vector3 initialScale = panel.transform.localScale;
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;

            // Smooth step down
            float scale = Mathf.Lerp(initialScale.x, 0f, Mathf.SmoothStep(0f, 1f, progress));
            panel.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        panel.transform.localScale = Vector3.zero;
        panel.SetActive(false);
        activeCoroutines.Remove(panel);
    }

    public void HidePanels()
    {
        foreach (var kvp in activeCoroutines)
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
            }
        }
        activeCoroutines.Clear();

        if (pauseMenu != null) { pauseMenu.transform.localScale = Vector3.one; pauseMenu.SetActive(false); }
        if (gameOverUI != null) { gameOverUI.transform.localScale = Vector3.one; gameOverUI.SetActive(false); }
        if (levelCompleteUI != null) { levelCompleteUI.transform.localScale = Vector3.one; levelCompleteUI.SetActive(false); }
        
        ResetPanelMaskAndDisable(leaderboardUI);
        if (editModeUI != null) { editModeUI.transform.localScale = Vector3.one; editModeUI.SetActive(false); }
        ResetPanelMaskAndDisable(tutorialPanel);
        ResetPanelMaskAndDisable(messagePanel);
    }

    private void ResetPanelMaskAndDisable(GameObject panel)
    {
        if (panel == null) return;
        panel.transform.localScale = Vector3.one;
        RectTransform scroll = GetScrollObjectFromPanel(panel);
        if (scroll != null)
        {
            RectMask2D mask = scroll.gameObject.GetComponent<RectMask2D>();
            if (mask != null)
            {
                mask.padding = Vector4.zero;
                mask.enabled = false;
            }
        }
        panel.SetActive(false);
    }
}
