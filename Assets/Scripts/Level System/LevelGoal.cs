using UnityEngine;

public class LevelGoal : MonoBehaviour
{
    private bool _triggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Ignore in level creator/playtest mode (handled by PlaytestGoalValidator instead)
        if (LevelCreatorUI.Instance != null) return;

        if (_triggered) return;
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;
        if (!collision.CompareTag("Player")) return;

        _triggered = true;

        // Defer CompleteLevel() to the next frame so we fully exit the physics callback
        // before the level prefab (which owns this object) gets destroyed.
        // Calling Destroy() on our own parent inside OnTriggerEnter2D freezes Unity's physics engine.
        StartCoroutine(CompleteLevelNextFrame());
    }

    private System.Collections.IEnumerator CompleteLevelNextFrame()
    {
        yield return null; // wait one frame to exit the physics step
        if (LevelManager.Instance != null)
            LevelManager.Instance.CompleteLevel();
    }
}
