/// <summary>
/// Player is on the ground and walking left or right.
/// Transitions → Idle (no input) | Jumping (jump pressed).
/// </summary>
public class PlayerWalkingState : PlayerStateBase
{
    public PlayerWalkingState(PlayerController controller) : base(controller) { }

    public override void Enter()
    {
        ctx.PlayAnimation("isWalking");
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayWalkingSound();
    }

    public override void Update()
    {
        ctx.ResetPlayerRotation();

        float dir = ctx.InputDirection;

        // ── Transition: jump ───────────────────────────────────────────────
        if (ctx.JumpRequested)
        {
            ctx.JumpRequested = false;
            // Apply horizontal flip before leaving so the sprite faces right way
            if (dir != 0f)
                ctx.PlayerSprite.flipX = (dir < 0);

            ctx.StateMachine.ChangeState(new PlayerJumpingState(ctx));
            return;
        }

        // ── Transition: no input → idle ───────────────────────────────────
        if (dir == 0f)
        {
            ctx.StateMachine.ChangeState(new PlayerIdleState(ctx));
            return;
        }

        // ── Horizontal movement ───────────────────────────────────────────
        ctx.PlayerTransform.Translate(
            UnityEngine.Vector3.right * dir * ctx.Speed * UnityEngine.Time.deltaTime);
        ctx.PlayerSprite.flipX = (dir < 0);
    }

    public override void Exit()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopWalkingSound();
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
