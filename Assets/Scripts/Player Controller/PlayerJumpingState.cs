/// <summary>
/// Player is airborne (either from a jump or from walking off a ledge).
/// Supports optional multi-jump if LevelManager.allowMultiJumps is set.
/// Transitions → Idle (land on Floor/Platform).
/// </summary>
public class PlayerJumpingState : PlayerStateBase
{
    private readonly bool _applyImpulse;
    private bool _hasDoubleJumped = false;

    /// <param name="applyImpulse">
    /// Pass <c>true</c> when the player pressed Jump.
    /// Pass <c>false</c> when the player walked off a ledge (no impulse needed).
    /// </param>
    public PlayerJumpingState(PlayerController controller, bool applyImpulse = true)
        : base(controller)
    {
        _applyImpulse = applyImpulse;
    }

    public override void Enter()
    {
        ctx.PlayAnimation("isJumping");

        if (_applyImpulse)
        {
            ctx.PlayerRigidbody.AddForce(
                UnityEngine.Vector2.up * ctx.JumpForce, UnityEngine.ForceMode2D.Impulse);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySfx("Jump");
        }
    }

    public override void Update()
    {
        // ── Multi-jump ────────────────────────────────────────────────────
        if (ctx.JumpRequested)
        {
            ctx.JumpRequested = false;
            if (ctx.IsMultiJump && !_hasDoubleJumped)
            {
                _hasDoubleJumped = true;
                ctx.PlayerRigidbody.AddForce(
                    UnityEngine.Vector2.up * ctx.JumpForce, UnityEngine.ForceMode2D.Impulse);
                ctx.PlayAnimation("isJumping");
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySfx("Jump");
            }
        }

        // ── Mid-air horizontal movement ───────────────────────────────────
        float dir = ctx.InputDirection;
        if (dir != 0f)
        {
            ctx.PlayerTransform.Translate(
                UnityEngine.Vector3.right * dir * ctx.Speed * UnityEngine.Time.deltaTime);
            ctx.PlayerSprite.flipX = (dir < 0);
        }
    }

    // ── Landing detection ─────────────────────────────────────────────────

    public override void OnTriggerEnter2D(UnityEngine.Collider2D other)
    {
        if (other.CompareTag("Floor"))
        {
            ctx.StateMachine.ChangeState(new PlayerIdleState(ctx));
        }
        else if (other.CompareTag("Death"))
        {
            ctx.StateMachine.ChangeState(new PlayerDeadState(ctx));
        }
    }

    public override void OnCollisionEnter2D(UnityEngine.Collision2D collision)
    {
        if (collision.gameObject.CompareTag("PlatformGround"))
        {
            ctx.StateMachine.ChangeState(new PlayerIdleState(ctx));
        }
    }
}
