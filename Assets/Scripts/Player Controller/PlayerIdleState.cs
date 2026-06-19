/// <summary>
/// Player is on the ground and not moving.
/// Transitions → Walking (input detected) | Jumping (jump pressed).
/// </summary>
public class PlayerIdleState : PlayerStateBase
{
    public PlayerIdleState(PlayerController controller) : base(controller) { }

    public override void Enter()
    {
        ctx.PlayAnimation("isIdle");
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopWalkingSound();
    }

    public override void Update()
    {
        ctx.ResetPlayerRotation();

        // ── Transition: jump ───────────────────────────────────────────────
        if (ctx.JumpRequested)
        {
            ctx.JumpRequested = false;
            ctx.StateMachine.ChangeState(new PlayerJumpingState(ctx));
            return;
        }

        // ── Transition: movement input ────────────────────────────────────
        if (ctx.InputDirection != 0f)
        {
            ctx.StateMachine.ChangeState(new PlayerWalkingState(ctx));
        }
    }

    public override void OnTriggerEnter2D(UnityEngine.Collider2D other)
    {
        if (other.CompareTag("Death"))
            ctx.StateMachine.ChangeState(new PlayerDeadState(ctx));
    }

    // Walking off a ledge (no jump pressed)
    public override void OnTriggerExit2D(UnityEngine.Collider2D other)
    {
        if (other.CompareTag("Floor"))
            ctx.StateMachine.ChangeState(new PlayerJumpingState(ctx, applyImpulse: false));
    }

    public override void OnCollisionExit2D(UnityEngine.Collision2D collision)
    {
        if (collision.gameObject.CompareTag("PlatformGround"))
            ctx.StateMachine.ChangeState(new PlayerJumpingState(ctx, applyImpulse: false));
    }
}
