using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public bool isPaused = false;
    public bool isGameOver= false;
    public bool isLevelCompleted = false;


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
        Time.timeScale = 1f;
        isPaused = false;
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
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ClosePauseMenu();
        }
    }

    public void GameOver()
    {
        Debug.Log("Game Over!");
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopAllSoundsExceptMusic();
            AudioManager.Instance.PlaySfx("GameOver");
        }
        UIManager.Instance.ToggleGameOverUI();
    }


    public void QuitGame()
    {
        string mainMenu = LevelManager.Instance.mainMenu;
        UIManager.Instance.HidePanels();
        SceneManager.LoadScene(mainMenu);
    }


}
