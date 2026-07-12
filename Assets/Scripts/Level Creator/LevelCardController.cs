using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class LevelCardController : MonoBehaviour
{
    [Header("Text Fields")]
    [Tooltip("Shows 'Level 1', 'Level 2' etc.")]
    [SerializeField] private TMP_Text levelNameText;
    [Tooltip("Shows 'Status: Live' or 'Status: Draft'")]
    [SerializeField] private TMP_Text statusText;
    [Tooltip("Shows 'Plays: 42' etc.")]
    [SerializeField] private TMP_Text playsText;
    [Tooltip("Shows 'Top Player: PlayerName'")]
    [SerializeField] private TMP_Text topPlayerText;

    [Header("Status Indicator")]
    [Tooltip("Image component used to colour the status indicator (green = Live, red = Draft).")]
    [SerializeField] private Image statusIndicator;

    [Header("Colours")]
    [SerializeField] private Color liveColor  = new Color(0.18f, 0.75f, 0.35f, 1f);
    [SerializeField] private Color draftColor = new Color(0.80f, 0.20f, 0.20f, 1f);

    [Header("Action Buttons")]
    [Tooltip("Button to open this level in the Level Creator for editing.")]
    [SerializeField] private Button editButton;
    [Tooltip("Button to share / copy the link for this level.")]
    [SerializeField] private Button shareButton;
    [Tooltip("Button to delete this level from local saves.")]
    [SerializeField] private Button deleteButton;
    [Tooltip("Button to directly load this level in playtest mode.")]
    [SerializeField] private Button playtestButton;

    // Runtime data
    private string saveKey;
    private CustomLevelData levelData;

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Initializes the card with the level's save key and data.
    /// Called by LevelListManager when spawning cards.
    /// </summary>
    public void Initialize(string key, CustomLevelData data)
    {
        saveKey   = key;
        levelData = data;

        // Level name
        if (levelNameText != null) levelNameText.text = data.levelName;

        // Status
        bool live = data.isLive;
        if (statusText != null)
        {
            statusText.text  = live ? "Status: Live" : "Status: Draft";
            statusText.color = live ? liveColor : draftColor;
        }
        if (statusIndicator != null)
        {
            statusIndicator.color = live ? liveColor : draftColor;
        }

        // Stats
        if (playsText != null)
            playsText.text = $"Plays: {data.playCount}";
        if (topPlayerText != null)
            topPlayerText.text = string.IsNullOrEmpty(data.topPlayer)
                ? "Top Player: —"
                : $"Top Player: {data.topPlayer}";

        // Wire buttons
        if (editButton != null)
        {
            editButton.onClick.RemoveAllListeners();
            editButton.onClick.AddListener(OpenInEditor);
        }
        if (shareButton != null)
        {
            shareButton.onClick.RemoveAllListeners();
            shareButton.onClick.AddListener(ShareLevel);
        }
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(DeleteLevel);
        }
        if (playtestButton != null)
        {
            playtestButton.onClick.RemoveAllListeners();
            playtestButton.onClick.AddListener(OpenInPlaytest);
        }
    }

    // ── Button actions ──────────────────────────────────────────────────────

    private void OpenInEditor()
    {
        // Tell LevelListManager which slot to load, then open the editor scene
        if (!string.IsNullOrEmpty(saveKey))
        {
            // Extract slot number from key (e.g. "CustomLevel_2" → 2)
            string slotStr = saveKey.Replace("CustomLevel_", "");
            if (int.TryParse(slotStr, out int slot))
            {
                PlayerPrefs.SetInt("EditLevelSlot", slot);
                PlayerPrefs.Save();
            }
        }

        Debug.Log($"[LevelCardController] Opening '{levelData?.levelName}' in Level Creator.");
        UnityEngine.SceneManagement.SceneManager.LoadScene("LevelCreator");
    }

    private void OpenInPlaytest()
    {
        // Tell LevelListManager which slot to load, and set the playtest flag
        if (!string.IsNullOrEmpty(saveKey))
        {
            string slotStr = saveKey.Replace("CustomLevel_", "");
            if (int.TryParse(slotStr, out int slot))
            {
                PlayerPrefs.SetInt("EditLevelSlot", slot);
                PlayerPrefs.SetInt("PlaytestOnLoad", 1);
                PlayerPrefs.Save();
            }
        }

        Debug.Log($"[LevelCardController] Opening '{levelData?.levelName}' in Level Creator in Playtest mode.");
        UnityEngine.SceneManagement.SceneManager.LoadScene("LevelCreator");
    }

    private void DeleteLevel()
    {
        if (string.IsNullOrEmpty(saveKey)) return;

        string slotStr = saveKey.Replace("CustomLevel_", "");
        if (int.TryParse(slotStr, out int slotToDelete))
        {
            DeleteSlotAndShiftDown(slotToDelete);
        }

        // Ask the list manager to refresh
        var manager = FindFirstObjectByType<LevelListManager>();
        if (manager != null)
        {
            manager.RefreshList();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static void DeleteSlotAndShiftDown(int slotToDelete)
    {
        int count = PlayerPrefs.GetInt("CustomLevel_Count", 0);
        if (slotToDelete < 1 || slotToDelete > count) return;

        // Shift everything above slotToDelete down by 1
        for (int i = slotToDelete + 1; i <= count; i++)
        {
            string sourceKey = $"CustomLevel_{i}";
            string destKey = $"CustomLevel_{i - 1}";

            if (PlayerPrefs.HasKey(sourceKey))
            {
                string json = PlayerPrefs.GetString(sourceKey, "");
                
                // Automatically adjust default Level naming inside JSON if it matches Level N
                try
                {
                    var data = JsonUtility.FromJson<CustomLevelData>(json);
                    if (data != null && data.levelName == $"Level {i}")
                    {
                        data.levelName = $"Level {i - 1}";
                        json = JsonUtility.ToJson(data, true);
                    }
                }
                catch { }

                PlayerPrefs.SetString(destKey, json);
            }
            else
            {
                PlayerPrefs.DeleteKey(destKey);
            }
        }

        // Delete the last slot since it has been shifted down
        PlayerPrefs.DeleteKey($"CustomLevel_{count}");

        // Decrement count
        int newCount = Mathf.Max(0, count - 1);
        PlayerPrefs.SetInt("CustomLevel_Count", newCount);
        PlayerPrefs.Save();

        Debug.Log($"[LevelCardController] Deleted slot {slotToDelete}. Count decremented from {count} to {newCount}.");
    }

    private void ShareLevel()
    {
        if (levelData == null) return;
        Debug.Log($"[LevelCardController] Share requested for '{levelData.levelName}'.");
        // TODO: wire to your Devvit share / copy-link flow
    }
}
