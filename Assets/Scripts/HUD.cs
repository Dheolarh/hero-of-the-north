using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    [Header("HUD Text")]
    public TextMeshProUGUI allyCounter;
    public TextMeshProUGUI retryCounter;
    public Button pauseButton;

    void OnEnable()
    {
        // Register this HUD instance with UIManager
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HUD = gameObject;
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnAlliesSavedChanged += UpdateAllyCounter;
            ScoreManager.Instance.OnRetryCountChanged += UpdateRetryCounter;

            // Immediately sync display to current values — events only fire on future
            // changes, so if values changed while HUD was hidden they would be stale
            UpdateAllyCounter(ScoreManager.Instance.alliesSaved);
            UpdateRetryCounter(ScoreManager.Instance.retryCount);
        }

        if (pauseButton != null && UIManager.Instance != null)
        {
            // Remove first to be absolutely safe, then add
            pauseButton.onClick.RemoveListener(UIManager.Instance.TogglePauseMenu);
            pauseButton.onClick.AddListener(UIManager.Instance.TogglePauseMenu);
        }
    }

    void OnDisable()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnAlliesSavedChanged -= UpdateAllyCounter;
            ScoreManager.Instance.OnRetryCountChanged -= UpdateRetryCounter;
        }

        if (pauseButton != null && UIManager.Instance != null)
        {
            pauseButton.onClick.RemoveListener(UIManager.Instance.TogglePauseMenu);
        }
    }

    void Start()
    {
        if (ScoreManager.Instance != null)
        {
            UpdateAllyCounter(ScoreManager.Instance.alliesSaved);
            UpdateRetryCounter(ScoreManager.Instance.retryCount);
        }
    }

    private void UpdateAllyCounter(int count)
    {
        if (allyCounter != null)
        {
            allyCounter.text = count.ToString();
        }
    }

    private void UpdateRetryCounter(int count)
    {
        if (retryCounter != null)
        {
            retryCounter.text = count.ToString();
        }
    }


}
