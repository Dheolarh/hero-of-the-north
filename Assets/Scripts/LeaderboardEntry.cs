using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardEntry : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private TextMeshProUGUI pointsText;
    [SerializeField] private Image avatarImage;

    [Header("Default Avatar")]
    [SerializeField] private Sprite defaultAvatar;

    public void SetData(int rank, string username, int points)
    {
        if (rankText != null)
        {
            rankText.text = $"{rank}";
        }
        else
        {
            Debug.LogWarning($"[LeaderboardEntry] rankText is NULL on {gameObject.name}");
        }

        if (usernameText != null)
        {
            usernameText.text = username;
        }
        else
        {
            Debug.LogWarning($"[LeaderboardEntry] usernameText is NULL on {gameObject.name}");
        }

        if (pointsText != null)
        {
            pointsText.text = $"{points}";
        }
        else
        {
            Debug.LogWarning($"[LeaderboardEntry] pointsText is NULL on {gameObject.name}");
        }

        // Set default avatar initially
        if (avatarImage != null)
        {
            if (defaultAvatar != null)
            {
                avatarImage.sprite = defaultAvatar;
                avatarImage.color = Color.white;
            }
            else
            {
                avatarImage.color = new Color(1f, 1f, 1f, 0f);
            }
        }
    }

    public void SetAvatar(Sprite avatar)
    {
        if (avatarImage != null && avatar != null)
        {
            avatarImage.sprite = avatar;
            avatarImage.color = new Color(1f, 1f, 1f, 1f);
        }
    }
}
