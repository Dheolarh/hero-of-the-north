using System.Collections;

/// <summary>
/// Player has hit a death trigger. Terminal state — no transitions out.
/// Handles all death consequences: animation, audio, camera stop, UI, restart.
/// </summary>
public class PlayerDeadState : PlayerStateBase
{
    public PlayerDeadState(PlayerController controller) : base(controller) { }

    public override void Enter()
    {
        // ── Mark game over ────────────────────────────────────────────────
        GameManager.Instance.isGameOver = true;

        // ── Animation ─────────────────────────────────────────────────────
        ctx.PlayAnimation("isDead");

        // ── Audio ─────────────────────────────────────────────────────────
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopAllSoundsExceptMusic();
            AudioManager.Instance.PlaySfx("Death");
        }

        // ── Camera ────────────────────────────────────────────────────────
        if (CameraShake.Instance != null && CameraShake.Instance.IsShaking())
            CameraShake.Instance.StopShake();

        ctx.StartCoroutine(StopCameraAfterDelay(1f));

        // ── Game Over UI & restart ────────────────────────────────────────
        GameManager.Instance.GameOver();
        ctx.StartCoroutine(RestartGame());
    }

    private IEnumerator StopCameraAfterDelay(float delay)
    {
        yield return new UnityEngine.WaitForSeconds(delay);
        if (ctx.CameraFollow != null)
            ctx.CameraFollow.StopFollowing();
    }

    private IEnumerator RestartGame()
    {
        yield return new UnityEngine.WaitForSeconds(2f);
        LevelManager.Instance.RestartLevel();
    }
}
