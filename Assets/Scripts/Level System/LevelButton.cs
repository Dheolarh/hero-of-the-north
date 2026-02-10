using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelButton : MonoBehaviour
{
    [SerializeField] private Image buttonImage;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Button button;

    private LevelData levelData;
    private bool isUnlocked;

    public void Initialize(LevelData data, bool unlocked)
    {
        levelData = data;
        isUnlocked = unlocked;

        if (buttonImage == null)
        {
            Debug.LogError($"[LevelButton] buttonImage is not assigned in the Inspector for {gameObject.name}!");
            return;
        }

        if (levelText == null)
        {
            Debug.LogError($"[LevelButton] levelText is not assigned in the Inspector for {gameObject.name}!");
            return;
        }

        if (button == null)
        {
            Debug.LogError($"[LevelButton] button is not assigned in the Inspector for {gameObject.name}!");
            return;
        }

        UpdateVisuals();

        // Add listener for both locked and unlocked levels
        button.onClick.AddListener(OnButtonClicked);
    }

    private void UpdateVisuals()
    {
        if (levelData.isBossLevel)
        {
            levelText.gameObject.SetActive(false);
            buttonImage.sprite = isUnlocked ? levelData.bossUnlockedIcon : levelData.bossLockedIcon;
        }
        else
        {
            levelText.gameObject.SetActive(true);
            levelText.text = levelData.levelName;
            buttonImage.sprite = isUnlocked ? levelData.levelIcon : levelData.lockedIcon;
        }

        buttonImage.color = Color.white;
    }

    private void OnButtonClicked()
    {
        if (LevelManager.Instance == null) return;

        if (isUnlocked)
        {
            // Level is unlocked, load it
            LevelManager.Instance.LoadLevel(levelData.levelNumber);
        }
        else
        {
            // Level is locked, check for unlock info and show countdown
            var unlockInfo = LevelManager.Instance.GetLevelUnlockInfo(levelData.levelNumber);

            if (unlockInfo != null && !unlockInfo.isUnlocked)
            {
                // Show countdown UI
                if (UIManager.Instance != null && UIManager.Instance.lockedLevelUI != null)
                {
                    UIManager.Instance.ToggleLockedLevelUI();

                    // Use GetComponentInChildren in case the script is on the child panel, not the root canvas
                    var countdown = UIManager.Instance.lockedLevelUI.GetComponentInChildren<LockedLevelCountdown>(true);
                    if (countdown != null)
                    {
                        countdown.ShowCountdown(unlockInfo);
                    }
                    else
                    {
                        Debug.LogWarning(
                            "[LevelButton] LockedLevelCountdown script not found in LockedLevelUI hierarchy!");
                    }
                }
            }
            else
            {
                Debug.Log($"Level {levelData.levelNumber} is locked (no server info available)");
            }
        }
    }
}
