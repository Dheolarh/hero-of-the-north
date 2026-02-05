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

        button.onClick.RemoveAllListeners();
        if (isUnlocked)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    private void UpdateVisuals()
    {
        if (levelData.isBossLevel)
        {
            levelText.gameObject.SetActive(false);

            if (isUnlocked)
            {
                buttonImage.sprite = levelData.bossUnlockedIcon;
            }
            else
            {
                buttonImage.sprite = levelData.bossLockedIcon;
            }
        }
        else
        {
            levelText.gameObject.SetActive(true);
            levelText.text = levelData.levelName;

            if (isUnlocked)
            {
                buttonImage.sprite = levelData.levelIcon;
            }
            else
            {
                buttonImage.sprite = levelData.lockedIcon;
            }
        }

        buttonImage.color = Color.white;
    }

    private void OnButtonClicked()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadLevel(levelData.levelNumber);
        }
    }
}
