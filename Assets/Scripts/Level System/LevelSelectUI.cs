using UnityEngine;
using System.Collections.Generic;

public class LevelSelectUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject levelButtonPrefab;
    [SerializeField] private Transform gridContainer;

    private List<LevelButton> levelButtons = new List<LevelButton>();

    [SerializeField] private UnityEngine.UI.ScrollRect scrollRect;

    void Start()
    {
        PopulateLevelGrid();
        ResetScrollPosition();
    }

    private void ResetScrollPosition()
    {
        if (scrollRect != null)
        {
            // Ensure layout generates before setting position
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f; // 1 = Top
        }
    }

    
    private void PopulateLevelGrid()
    {
        if (LevelManager.Instance == null)
        {
            Debug.LogError("LevelManager not found!");
            return;
        }

        Debug.Log("LevelManager found! Populating grid...");

        foreach (Transform child in gridContainer)
        {
            Destroy(child.gameObject);
        }
        levelButtons.Clear();

        List<LevelData> allLevels = LevelManager.Instance.GetAllLevels();
        Debug.Log($"Found {allLevels.Count} levels in LevelManager");

        if (allLevels.Count == 0)
        {
            Debug.LogWarning("No levels assigned to LevelManager! Please add LevelData assets to the 'All Levels' list in the LevelManager Inspector.");
            return;
        }

        foreach (LevelData levelData in allLevels)
        {
            if (levelData == null)
            {
                Debug.LogWarning("Null LevelData found in allLevels list!");
                continue;
            }

            Debug.Log($"Creating button for Level {levelData.levelNumber}: {levelData.levelName}");
            GameObject buttonObj = Instantiate(levelButtonPrefab, gridContainer);
            LevelButton levelButton = buttonObj.GetComponent<LevelButton>();
            
            if (levelButton != null)
            {
                bool isUnlocked = LevelManager.Instance.IsLevelUnlocked(levelData.levelNumber);
                levelButton.Initialize(levelData, isUnlocked);
                levelButtons.Add(levelButton);
                Debug.Log($"Button created for Level {levelData.levelNumber} - Unlocked: {isUnlocked}");
            }
            else
            {
                Debug.LogError("LevelButton component not found on prefab!");
            }
        }

        Debug.Log($"Total buttons created: {levelButtons.Count}");
    }

    public void RefreshUI()
    {
        PopulateLevelGrid();
    }
}
