using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

    public void TogglePauseMenu() => pauseMenu.SetActive(!pauseMenu.activeSelf);
    public void ToggleGameOverUI() => gameOverUI.SetActive(!gameOverUI.activeSelf);
    public void ToggleLevelCompleteUI() => levelCompleteUI.SetActive(!levelCompleteUI.activeSelf);
    public void ToggleLeaderboardUI() => leaderboardUI.SetActive(!leaderboardUI.activeSelf);
    public void ToggleLockedLevelUI() => lockedLevelUI.SetActive(!lockedLevelUI.activeSelf);
    public void ToggleHUD() => HUD.SetActive(!HUD.activeSelf);
}