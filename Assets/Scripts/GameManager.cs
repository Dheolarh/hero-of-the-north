using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public bool isPaused = false;
    public bool isGameOver= false;
    public bool isLevelCompleted = false;
    [SerializeField] ScrollRect levelRect;

    void Awake()
    {
        // Singleton pattern - ensure only one instance exists
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
        StartCoroutine(ResetScrollPosition());
    }

    // ========== GAME STATE METHODS ==========

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
    }

    public void GameOver()
    {
        Debug.Log("Game Over!");
        
        // Stop all sounds except background music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopAllSoundsExceptMusic();
            AudioManager.Instance.PlaySfx("GameOver");
        }
        UIManager.Instance.ToggleGameOverUI();
        
        LevelManager.Instance.RestartLevel();
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }

    private IEnumerator ResetScrollPosition()
    {
        yield return new WaitForEndOfFrame();
        
        if (levelRect != null)
        {
            levelRect.verticalNormalizedPosition = 1f; // 1 = top, 0 = bottom
        }
    }
}
