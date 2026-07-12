using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages spawning and refreshing community published levels inside the Community Panel's Scroll View.
/// </summary>
public class CommunityListManager : MonoBehaviour
{
    [Header("Spawning References")]
    [Tooltip("The card prefab to spawn (e.g. 'Community Levels' prefab).")]
    [SerializeField] private GameObject communityCardPrefab;
    [Tooltip("The Content container of the Scroll View where cards will be instantiated.")]
    [SerializeField] private Transform cardContainer;

    void Awake()
    {
        // Programmatically locate cardContainer if not set
        if (cardContainer == null)
        {
            var uiMgr = FindFirstObjectByType<UIManager>();
            if (uiMgr != null && uiMgr.communityPanel != null)
            {
                var contentTransform = uiMgr.communityPanel.transform.Find("showcase/SectionB/Scroll View/Viewport/Content");
                if (contentTransform != null)
                {
                    cardContainer = contentTransform;
                    Debug.Log("[CommunityListManager] Programmatically located cardContainer: " + cardContainer.name);
                }
            }
        }

        // Programmatically resolve prefab by using the first child in cardContainer as a template if not assigned
        if (communityCardPrefab == null && cardContainer != null && cardContainer.childCount > 0)
        {
            var template = cardContainer.GetChild(0).gameObject;
            communityCardPrefab = template;
            template.SetActive(false); // Hide the template so it's not visible
            Debug.Log("[CommunityListManager] Programmatically using first child as communityCardPrefab template: " + template.name);
        }
    }

    /// <summary>
    /// Configures the card prefab and container from external managers (like UIManager).
    /// </summary>
    public void Setup(GameObject prefab, Transform container)
    {
        if (prefab != null) communityCardPrefab = prefab;
        if (container != null) cardContainer = container;
    }

    void OnEnable()
    {
        RefreshCommunityList();
    }

    /// <summary>
    /// Fetches the published levels from Devvit and rebuilds the card list.
    /// </summary>
    public void RefreshCommunityList()
    {
        if (cardContainer == null)
        {
            Debug.LogWarning("[CommunityListManager] cardContainer is not assigned.");
            return;
        }

        // Clear existing spawned cards (except the template card if used as prefab)
        foreach (Transform child in cardContainer)
        {
            if (communityCardPrefab != null && child.gameObject == communityCardPrefab)
                continue;

            Destroy(child.gameObject);
        }

        if (DevvitBridge.Instance == null)
        {
            Debug.LogWarning("[CommunityListManager] DevvitBridge not found! Cannot fetch community levels.");
            return;
        }

        DevvitBridge.Instance.RequestCommunityLevels((levels) =>
        {
            if (levels == null || levels.Length == 0)
            {
                Debug.Log("[CommunityListManager] No community levels received.");
                return;
            }

            // Assign sequential "Level X" names based on creator's publish order (assuming array is chronological)
            Dictionary<string, int> creatorCounts = new Dictionary<string, int>();
            foreach (var lvl in levels)
            {
                if (lvl == null) continue;
                if (!creatorCounts.ContainsKey(lvl.creator))
                    creatorCounts[lvl.creator] = 0;
                
                creatorCounts[lvl.creator]++;
                lvl.levelName = $"Level {creatorCounts[lvl.creator]}";
            }

            // Sort levels by most plays (descending)
            System.Array.Sort(levels, (a, b) => b.playCount.CompareTo(a.playCount));

            foreach (var lvl in levels)
            {
                if (lvl == null) continue;

                if (communityCardPrefab == null)
                {
                    Debug.LogError("[CommunityListManager] communityCardPrefab is not assigned!");
                    break;
                }

                GameObject cardObj = Instantiate(communityCardPrefab, cardContainer);
                cardObj.SetActive(true); // Make sure the clone is active
                var cardCtrl = cardObj.GetComponent<CommunityCardController>();
                if (cardCtrl == null)
                {
                    cardCtrl = cardObj.AddComponent<CommunityCardController>();
                }
                
                // Clean up any "u/" prefixes that Reddit might return
                string cleanCreator = DevvitBridge.TrimUsername(lvl.creator, "Unknown");
                string cleanTopPlayer = string.IsNullOrEmpty(lvl.topPlayer) ? "" : DevvitBridge.TrimUsername(lvl.topPlayer, "");

                cardCtrl.Initialize(lvl.levelName, cleanCreator, lvl.playCount, cleanTopPlayer, lvl.levelData, lvl.avatarUrl);
            }

            Debug.Log($"[CommunityListManager] Successfully populated {levels.Length} community levels.");
        });
    }
}
