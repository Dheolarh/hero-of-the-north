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
        }
        pauseButton.onClick.AddListener(UIManager.Instance.TogglePauseMenu);
    }

    void OnDisable()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnAlliesSavedChanged -= UpdateAllyCounter;
            ScoreManager.Instance.OnRetryCountChanged -= UpdateRetryCounter;
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
