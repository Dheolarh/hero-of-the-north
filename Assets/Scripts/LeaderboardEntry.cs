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
            rankText.text = $"#{rank}";
        }

        if (usernameText != null)
        {
            usernameText.text = username;
        }

        if (pointsText != null)
        {
            pointsText.text = $"{points} pts";
        }

        // Set default avatar initially
        if (avatarImage != null && defaultAvatar != null)
        {
            avatarImage.sprite = defaultAvatar;
        }
    }

    public void SetAvatar(Sprite avatar)
    {
        if (avatarImage != null && avatar != null)
        {
            avatarImage.sprite = avatar;
        }
    }
}
