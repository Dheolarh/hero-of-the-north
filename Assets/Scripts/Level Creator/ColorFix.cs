using UnityEngine;

/// <summary>
/// Controls the visibility of editor-only helper objects (like Trigger Zones).
/// Makes them visible with a custom color in Editor mode, but fully invisible during Playtest and Game modes.
/// </summary>
public class ColorFix : MonoBehaviour
{
    [Tooltip("The color of the object in Editor mode (opacity will be forced to 1).")]
    [SerializeField] private Color editorColor = new Color(0.2f, 0.6f, 1f, 0.5f);

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    void Start()
    {
        UpdateVisuals();

        // Listen for playtest toggle events to swap visibility dynamically in the editor
        if (LevelCreatorUI.Instance != null)
        {
            LevelCreatorUI.Instance.OnPlaytestToggled += OnPlaytestToggled;
        }
    }

    void OnDestroy()
    {
        if (LevelCreatorUI.Instance != null)
        {
            LevelCreatorUI.Instance.OnPlaytestToggled -= OnPlaytestToggled;
        }
    }

    private void OnPlaytestToggled(bool isPlaytesting)
    {
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer == null) return;

        bool isEditorMode = LevelCreatorUI.Instance != null && !LevelCreatorUI.Instance.IsPlaytesting;

        if (isEditorMode)
        {
            // In editor mode, force opacity to 1 and apply the preferred color
            Color c = editorColor;
            c.a = 1f; 
            spriteRenderer.color = c;
            spriteRenderer.enabled = true;
        }
        else
        {
            // In game mode or playtest mode, make it completely invisible
            Color c = spriteRenderer.color;
            c.a = 0f;
            spriteRenderer.color = c;
            spriteRenderer.enabled = false;
        }
    }
}
