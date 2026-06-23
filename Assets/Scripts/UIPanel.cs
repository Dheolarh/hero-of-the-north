using UnityEngine;

/// <summary>
/// Attach this to each UI panel in the Main/Game scene so UIManager can locate and register it
/// when the scene loads. This keeps UIManager's references fresh after scene reloads.
/// </summary>
public class UIPanel : MonoBehaviour
{
    public enum PanelType
    {
        PauseMenu,
        GameOverUI,
        LevelCompleteUI,
        LeaderboardUI,
        LockedLevelUI,
        TutorialPanel,
        MessagePanel
    }

    [SerializeField] private PanelType panelType;

    public PanelType Type => panelType;
}

