using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;


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
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Menu") HUD.SetActive(false);
    }

    private Dictionary<GameObject, Coroutine> activeCoroutines =
        new Dictionary<GameObject, Coroutine>();

    public void TogglePauseMenu() => TogglePanel(pauseMenu);
    public void ToggleGameOverUI() => TogglePanel(gameOverUI);
    public void ToggleLevelCompleteUI() => TogglePanel(levelCompleteUI);
    public void ToggleLeaderboardUI() => TogglePanel(leaderboardUI);
    public void ToggleLockedLevelUI() => TogglePanel(lockedLevelUI);

    // HUD should be constant/instant
    public void ToggleHUD()
    {
        if (activeCoroutines.ContainsKey(HUD))
        {
            StopCoroutine(activeCoroutines[HUD]);
            activeCoroutines.Remove(HUD);
        }

        HUD.transform.localScale = Vector3.one;
        HUD.SetActive(!HUD.activeSelf);
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
}