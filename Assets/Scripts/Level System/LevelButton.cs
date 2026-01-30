using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelButton : MonoBehaviour
{
    [SerializeField] private Image buttonImage;
    [SerializeField] private Image lockOverlay;
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
        else
        {
            button.interactable = false;
        }
    }

    private void UpdateVisuals()
    {
        Debug.Log($"[LevelButton] {levelData.levelName} - Unlocked: {isUnlocked}, Boss: {levelData.isBossLevel}");
        
        // Handle boss levels specially
        if (levelData.isBossLevel)
        {
            // Boss levels: hide level number text, show boss icon
            if (levelText != null)
            {
                levelText.gameObject.SetActive(false); // Hide the text completely
            }
            
            // Always show boss icon for boss levels (whether locked or unlocked)
            if (levelData.bossLevelIcon != null)
            {
                buttonImage.sprite = levelData.bossLevelIcon;
                Debug.Log($"[LevelButton] Set boss level icon for {levelData.levelName}");
            }
            else
            {
                Debug.LogWarning($"[LevelButton] No bossLevelIcon assigned for boss level {levelData.levelName}!");
            }
            
            if (isUnlocked)
            {
                // Unlocked boss level: hide lock overlay
                if (lockOverlay != null)
                {
                    lockOverlay.gameObject.SetActive(false);
                }
            }
            else
            {
                // Locked boss level: show lock overlay on top of boss icon
                if (lockOverlay != null)
                {
                    lockOverlay.gameObject.SetActive(true);
                    Color overlayColor = lockOverlay.color;
                    overlayColor.a = 0.7f; // 70% opacity
                    lockOverlay.color = overlayColor;
                }
            }
        }
        else
        {
            // Normal levels: show level number
            if (levelText != null)
            {
                levelText.gameObject.SetActive(true); // Make sure text is visible
                levelText.text = levelData.levelName;
            }
            
            if (isUnlocked)
            {
                // Unlocked: show level icon, hide lock overlay
                if (levelData.levelIcon != null)
                {
                    buttonImage.sprite = levelData.levelIcon;
                    Debug.Log($"[LevelButton] Set unlocked icon for {levelData.levelName}");
                }
                else
                {
                    Debug.LogWarning($"[LevelButton] No levelIcon assigned for {levelData.levelName}!");
                }
                
                if (lockOverlay != null)
                {
                    lockOverlay.gameObject.SetActive(false);
                    Debug.Log($"[LevelButton] Hiding lock overlay for {levelData.levelName}");
                }
            }
            else
            {
                // Locked: show locked icon as background, show semi-transparent lock overlay
                if (levelData.lockedIcon != null)
                {
                    buttonImage.sprite = levelData.lockedIcon;
                }
                
                if (lockOverlay != null)
                {
                    lockOverlay.gameObject.SetActive(true);
                    // Make it semi-transparent (you can adjust the alpha value)
                    Color overlayColor = lockOverlay.color;
                    overlayColor.a = 0.7f; // 70% opacity
                    lockOverlay.color = overlayColor;
                }
            }
        }
    }

    private void OnButtonClicked()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadLevel(levelData.levelNumber);
        }
    }
}
