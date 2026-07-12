using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Controller for each community level card spawned in the Main scene's Community panel.
/// </summary>
public class CommunityCardController : MonoBehaviour
{
    [Header("UI Fields")]
    [SerializeField] private TMP_Text levelNameText;
    [SerializeField] private TMP_Text creatorText;
    [SerializeField] private TMP_Text playsText;
    [SerializeField] private TMP_Text topPlayerText;
    [SerializeField] private Button playButton;

    [Header("Creator Snoovatar (Avatar)")]
    [Tooltip("The Image component displaying the level creator's Reddit avatar.")]
    [SerializeField] private Image snoovatarImage;
    [Tooltip("Fallback Sprite if the creator has no custom avatar url.")]
    [SerializeField] private Sprite defaultAvatar;

    private string levelDataJson;
    private static System.Collections.Generic.Dictionary<string, Sprite> avatarCache = new System.Collections.Generic.Dictionary<string, Sprite>();

    void Awake()
    {
        // Fallbacks for unassigned components
        if (levelNameText == null) levelNameText = transform.Find("Info/Status")?.GetComponent<TMP_Text>() ?? transform.Find("Info/Name")?.GetComponent<TMP_Text>() ?? GetComponentInChildren<TMP_Text>();
        if (creatorText == null) creatorText = transform.Find("Info/Top Player")?.GetComponent<TMP_Text>() ?? transform.Find("Info/Creator")?.GetComponent<TMP_Text>();
        if (playsText == null) playsText = transform.Find("Info/Play Count")?.GetComponent<TMP_Text>();
        if (playButton == null) playButton = GetComponent<Button>() ?? GetComponentInChildren<Button>();
        if (snoovatarImage == null) snoovatarImage = transform.Find("Snoovatar")?.GetComponent<Image>() ?? transform.Find("Avatar")?.GetComponent<Image>() ?? transform.Find("Image")?.GetComponent<Image>();
    }

    /// <summary>
    /// Populates the card UI with community level details and starts loading the creator's avatar.
    /// </summary>
    public void Initialize(string levelName, string creatorName, int playCount, string topPlayer, string json, string avatarUrl)
    {
        if (levelNameText != null) levelNameText.text = levelName;
        if (creatorText != null) creatorText.text = $"by {creatorName}";
        if (playsText != null) playsText.text = $"Plays: {playCount}";
        if (topPlayerText != null)
        {
            topPlayerText.text = string.IsNullOrEmpty(topPlayer) ? "Top Player: —" : $"Top Player: {topPlayer}";
        }

        levelDataJson = json;

        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(PlayLevel);
        }

        // Initialize/reset avatar component
        if (snoovatarImage != null)
        {
            if (defaultAvatar != null)
            {
                snoovatarImage.sprite = defaultAvatar;
                snoovatarImage.color = Color.white;
            }
            else
            {
                snoovatarImage.sprite = null;
                snoovatarImage.color = new Color(1f, 1f, 1f, 0f); // transparent if no default configured
            }

            if (!string.IsNullOrEmpty(avatarUrl))
            {
                StartCoroutine(LoadAvatar(avatarUrl));
            }
        }
    }

    private IEnumerator LoadAvatar(string url)
    {
        if (avatarCache.ContainsKey(url))
        {
            if (snoovatarImage != null)
            {
                snoovatarImage.sprite = avatarCache[url];
                snoovatarImage.color = Color.white;
            }
            yield break;
        }

        using UnityWebRequest req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Texture2D tex = ((DownloadHandlerTexture)req.downloadHandler).texture;
            if (tex != null)
            {
                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                avatarCache[url] = sprite;

                if (snoovatarImage != null)
                {
                    snoovatarImage.sprite = sprite;
                    snoovatarImage.color = Color.white;
                }
            }
        }
        else
        {
            Debug.LogWarning($"[CommunityCardController] Failed to download snoovatar from '{url}': {req.error}");
        }
    }

    private void PlayLevel()
    {
        if (string.IsNullOrEmpty(levelDataJson)) return;

        PlayerPrefs.SetString("PlayCommunityLevelJSON", levelDataJson);
        PlayerPrefs.Save();

        Debug.Log($"[CommunityCardController] Launching community level '{levelNameText?.text}' in direct Playtest mode.");
        UnityEngine.SceneManagement.SceneManager.LoadScene("LevelCreator");
    }
}
