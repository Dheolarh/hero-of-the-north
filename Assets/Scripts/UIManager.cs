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
        // If the object is a Canvas, animate its first child (the actual panel)
        Transform targetTransform = panel.transform;
        if (panel.GetComponent<Canvas>() != null && panel.transform.childCount > 0)
        {
            targetTransform = panel.transform.GetChild(0);
        }

        targetTransform.localScale = Vector3.zero;

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time so it works when paused
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

            targetTransform.localScale = Vector3.one * scale;
            yield return null;
        }

        targetTransform.localScale = Vector3.one;
        activeCoroutines.Remove(panel);
    }

    private IEnumerator AnimateHide(GameObject panel)
    {
        // If the object is a Canvas, animate its first child
        Transform targetTransform = panel.transform;
        if (panel.GetComponent<Canvas>() != null && panel.transform.childCount > 0)
        {
            targetTransform = panel.transform.GetChild(0);
        }

        Vector3 initialScale = targetTransform.localScale;

        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;

            // Smooth step down
            float scale = Mathf.Lerp(initialScale.x, 0f, Mathf.SmoothStep(0f, 1f, progress));
            targetTransform.localScale = Vector3.one * scale;
            yield return null;
        }

        targetTransform.localScale = Vector3.zero;
        panel.SetActive(false);

        // Reset scale so it's ready for next show (important if we want to show it again)
        // Actually, ShowAnimate sets it to 0 at start, so it's fine.
        // But let's reset to 1 just in case logic changes? 
        // No, leave it zero to avoid flash. ShowAnimate handles it.

        activeCoroutines.Remove(panel);
    }
}