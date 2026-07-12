using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the Level Creator menu screen's level list.
/// Spawns a LevelCardController prefab for every saved level.
/// Also handles "Create New Level" button logic.
/// </summary>
public class LevelListManager : MonoBehaviour
{
    [Header("Level Card Spawning")]
    [Tooltip("The prefab that represents one level in the list. Must have a LevelCardController component.")]
    [SerializeField] private GameObject levelCardPrefab;
    [Tooltip("The container (Content transform of a ScrollRect) where cards are spawned.")]
    [SerializeField] private Transform cardContainer;

    [Header("Create New Level")]
    [Tooltip("Button that creates the next level slot and opens the Level Creator.")]
    [SerializeField] private Button createNewLevelButton;
    [Tooltip("Name of the Level Creator scene to load.")]
    [SerializeField] private string levelCreatorSceneName = "LevelCreator";

    [Header("Empty State")]
    [Tooltip("Optional: shown when no levels are saved yet.")]
    [SerializeField] private GameObject emptyStatePanel;

    private readonly List<GameObject> spawnedCards = new List<GameObject>();

    // ── Unity lifecycle ─────────────────────────────────────────────────────

    void Awake()
    {
        if (createNewLevelButton != null)
        {
            createNewLevelButton.onClick.RemoveAllListeners();
            createNewLevelButton.onClick.AddListener(CreateNewLevel);
        }
    }

    void OnEnable()
    {
        RefreshList();
    }

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>Destroys and rebuilds all level cards from PlayerPrefs.</summary>
    public void RefreshList()
    {
        // Consolidate slots to heal any gaps from legacy saves
        ConsolidateSaveSlots();

        // Clear old cards
        foreach (var card in spawnedCards)
            if (card != null) Destroy(card);
        spawnedCards.Clear();

        var keys = GetAllSavedLevelKeys();

        if (emptyStatePanel != null)
            emptyStatePanel.SetActive(keys.Count == 0);

        foreach (string key in keys)
        {
            string json = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(json)) continue;

            CustomLevelData data;
            try   { data = JsonUtility.FromJson<CustomLevelData>(json); }
            catch { Debug.LogWarning($"[LevelListManager] Could not parse {key}"); continue; }

            if (data == null) continue;

            if (levelCardPrefab == null)
            {
                Debug.LogWarning("[LevelListManager] levelCardPrefab is not assigned!");
                break;
            }

            GameObject cardObj = Instantiate(levelCardPrefab, cardContainer);
            var card = cardObj.GetComponent<LevelCardController>();
            if (card != null)
                card.Initialize(key, data);

            spawnedCards.Add(cardObj);
        }

        Debug.Log($"[LevelListManager] Loaded {spawnedCards.Count} level card(s).");
    }

    // ── Create New Level ────────────────────────────────────────────────────

    /// <summary>
    /// Creates the next level slot in PlayerPrefs and opens the Level Creator.
    /// Called by the "Create New Level" button.
    /// </summary>
    public void CreateNewLevel()
    {
        int nextSlot = PlayerPrefs.GetInt("CustomLevel_Count", 0) + 1;
        string levelName = $"Level {nextSlot}";
        string saveKey   = $"CustomLevel_{nextSlot}";

        // Persist the new empty level so it exists in the list
        var emptyData = new CustomLevelData
        {
            levelName = levelName,
            creator   = "EditorPlayer",
            isLive    = false
        };
        string json = JsonUtility.ToJson(emptyData, true);
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.SetInt("CustomLevel_Count", nextSlot);

        // Tell the Level Creator which slot to edit
        PlayerPrefs.SetInt("EditLevelSlot", nextSlot);
        PlayerPrefs.Save();

        Debug.Log($"[LevelListManager] Creating new level: {levelName} (slot {nextSlot}).");
        UnityEngine.SceneManagement.SceneManager.LoadScene(levelCreatorSceneName);
    }

    // ── Save key helpers ────────────────────────────────────────────────────

    private void ConsolidateSaveSlots()
    {
        int count = PlayerPrefs.GetInt("CustomLevel_Count", 0);
        int currentNewIndex = 1;

        for (int i = 1; i <= count; i++)
        {
            string oldKey = $"CustomLevel_{i}";
            if (PlayerPrefs.HasKey(oldKey))
            {
                string json = PlayerPrefs.GetString(oldKey, "");

                // If this slot has a gap behind it, shift it down to the next contiguous slot
                if (i != currentNewIndex)
                {
                    string newKey = $"CustomLevel_{currentNewIndex}";

                    // Rename default name inside JSON if it matches 'Level N'
                    try
                    {
                        var data = JsonUtility.FromJson<CustomLevelData>(json);
                        if (data != null && data.levelName == $"Level {i}")
                        {
                            data.levelName = $"Level {currentNewIndex}";
                            json = JsonUtility.ToJson(data, true);
                        }
                    }
                    catch { }

                    PlayerPrefs.SetString(newKey, json);
                    PlayerPrefs.DeleteKey(oldKey);
                    Debug.Log($"[LevelListManager] Consolidate: Moved slot {i} -> {currentNewIndex}");
                }

                currentNewIndex++;
            }
        }

        int finalCount = currentNewIndex - 1;
        if (finalCount != count)
        {
            PlayerPrefs.SetInt("CustomLevel_Count", finalCount);
            PlayerPrefs.Save();
            Debug.Log($"[LevelListManager] Consolidate: Adjusted CustomLevel_Count from {count} to {finalCount}");
        }
    }

    private static List<string> GetAllSavedLevelKeys()
    {
        int count = PlayerPrefs.GetInt("CustomLevel_Count", 0);
        var keys  = new List<string>();
        for (int i = 1; i <= count; i++)
        {
            string k = $"CustomLevel_{i}";
            if (PlayerPrefs.HasKey(k)) keys.Add(k);
        }
        return keys;
    }
}
