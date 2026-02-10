using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays countdown timer for locked levels
/// Shows format: Level X - DD:HH:MM:SS or HH:MM:SS depending on time remaining
/// </summary>
public class LockedLevelCountdown : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI countdownText;

    private DevvitBridge.LevelUnlockInfo currentLevelInfo;
    private bool isActive = false;

    void Update()
    {
        if (isActive && currentLevelInfo != null && !currentLevelInfo.isUnlocked)
        {
            UpdateCountdown();
        }
    }

    /// <summary>
    /// Show countdown for a specific locked level
    /// </summary>
    public void ShowCountdown(DevvitBridge.LevelUnlockInfo levelInfo)
    {
        if (levelInfo.isUnlocked)
        {
            Debug.LogWarning($"[LockedLevelCountdown] Level {levelInfo.levelNumber} is already unlocked!");
            return;
        }

        currentLevelInfo = levelInfo;
        isActive = true;

        // Initial countdown update
        UpdateCountdown();

        // Show the UI
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Hide the countdown UI
    /// </summary>
    public void HideCountdown()
    {
        isActive = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Update the countdown display
    /// </summary>
    private void UpdateCountdown()
    {
        if (currentLevelInfo == null || countdownText == null)
            return;

        // Calculate time remaining (server time - current time)
        long currentTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long timeRemainingMs = currentLevelInfo.unlockTime - currentTimeMs;

        if (timeRemainingMs <= 0)
        {
            // Level should be unlocked now
            countdownText.text = "Unlocking...";
            
            // Request fresh unlock data from server
            if (DevvitBridge.Instance != null)
            {
                DevvitBridge.Instance.RequestUnlockedLevels();
            }
            
            return;
        }

        // Convert to seconds
        long totalSeconds = timeRemainingMs / 1000;
        
        // Calculate time components
        long days = totalSeconds / (24 * 3600);
        long hours = (totalSeconds % (24 * 3600)) / 3600;
        long minutes = (totalSeconds % 3600) / 60;
        long seconds = totalSeconds % 60;

        // Format based on time remaining
        string formattedTime;
        if (days > 0)
        {
            // Show DD:HH:MM:SS for more than 1 day
            formattedTime = $"{days:D2}:{hours:D2}:{minutes:D2}:{seconds:D2}";
        }
        else
        {
            // Show HH:MM:SS for less than 1 day
            formattedTime = $"{hours:D2}:{minutes:D2}:{seconds:D2}";
        }

        countdownText.text = formattedTime;
    }

    /// <summary>
    /// Refresh countdown with updated level info
    /// </summary>
    public void RefreshCountdown(DevvitBridge.LevelUnlockInfo updatedLevelInfo)
    {
        if (currentLevelInfo != null && currentLevelInfo.levelNumber == updatedLevelInfo.levelNumber)
        {
            currentLevelInfo = updatedLevelInfo;
            
            if (updatedLevelInfo.isUnlocked)
            {
                // Level is now unlocked, hide countdown
                HideCountdown();
            }
        }
    }
}
