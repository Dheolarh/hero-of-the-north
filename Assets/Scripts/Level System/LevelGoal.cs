using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelGoal : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool loadNextLevelOnComplete = true;
    [SerializeField] private float delayBeforeNextLevel = 1.5f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            CompleteLevel();
        }
    }

    private void CompleteLevel()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.CompleteLevel();
            
            if (loadNextLevelOnComplete)
            {
                Invoke(nameof(LoadNext), delayBeforeNextLevel);
            }
        }
    }

    private void LoadNext()
    {
        LevelManager.Instance.LoadNextLevel();
    }
}
