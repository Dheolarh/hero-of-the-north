/// <summary>
/// Player is airborne (either from a jump or from walking off a ledge).
/// Supports optional multi-jump if LevelManager.allowMultiJumps is set.
/// Transitions → Idle (land on Floor/Platform).
/// </summary>
public class PlayerJumpingState : PlayerStateBase
{
    private readonly bool _applyImpulse;
    private int _midAirJumpsPerformed = 0;

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
        ctx.ResetPlayerRotation();

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
        ctx.ResetPlayerRotation();

        // ── Grounded check fallback ───────────────────────────────────────
        if (ctx.PlayerRigidbody.linearVelocity.y <= 0.1f && ctx.IsGrounded())
        {
            ctx.StateMachine.ChangeState(new PlayerIdleState(ctx));
            return;
        }

        // ── Multi-jump ────────────────────────────────────────────────────
        if (ctx.JumpRequested)
        {
            ctx.JumpRequested = false;
            if (_midAirJumpsPerformed < ctx.MaxMultiJumps)
            {
                _midAirJumpsPerformed++;
                
                // Reset vertical velocity so upward momentum doesn't stack and multiply the jump height
                ctx.PlayerRigidbody.linearVelocity = new UnityEngine.Vector2(ctx.PlayerRigidbody.linearVelocity.x, 0f);

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
        if (other.CompareTag("Floor") || other.CompareTag("PlatformGround"))
        {
            // Only land if the player is falling or stationary (not moving upwards)
            if (ctx.PlayerRigidbody.linearVelocity.y <= 0.1f)
            {
                ctx.StateMachine.ChangeState(new PlayerIdleState(ctx));
            }
        }
        else if (other.CompareTag("Death"))
        {
            ctx.StateMachine.ChangeState(new PlayerDeadState(ctx));
        }
    }

    public override void OnCollisionEnter2D(UnityEngine.Collision2D collision)
    {
        CheckLanding(collision);
    }

    public override void OnCollisionStay2D(UnityEngine.Collision2D collision)
    {
        CheckLanding(collision);
    }

    private void CheckLanding(UnityEngine.Collision2D collision)
    {
        if (collision.gameObject.CompareTag("PlatformGround") || collision.gameObject.CompareTag("Floor"))
        {
            // Only land if we hit the top of the platform/ground (contact normal points upwards)
            if (collision.contactCount > 0)
            {
                foreach (var contact in collision.contacts)
                {
                    if (contact.normal.y > 0.5f)
                    {
                        ctx.StateMachine.ChangeState(new PlayerIdleState(ctx));
                        break;
                    }
                }
            }
        }
    }
}
