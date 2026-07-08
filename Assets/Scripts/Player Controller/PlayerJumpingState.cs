using UnityEngine;

/// <summary>
/// Player is airborne (either from a jump or from walking off a ledge).
/// Supports optional multi-jump if LevelManager.allowMultiJumps is set.
/// Transitions → Idle (land on Floor/Platform).
/// </summary>
public class PlayerJumpingState : PlayerStateBase
{
    private readonly bool _applyImpulse;
    private int _midAirJumpsPerformed = 0;
    private float _highestAirborneY;

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
        
        // Track height from the starting position of the jump/fall
        _highestAirborneY = ctx.PlayerTransform.position.y;

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

        // Dynamically update the peak Y coordinate reached while airborne
        if (ctx.PlayerTransform.position.y > _highestAirborneY)
        {
            _highestAirborneY = ctx.PlayerTransform.position.y;
        }

        // ── Grounded check fallback ───────────────────────────────────────
        if (ctx.PlayerRigidbody.linearVelocity.y <= 0.1f && ctx.IsGrounded())
        {
            LandPlayer();
            return;
        }

        // ── Multi-jump ────────────────────────────────────────────────────
        if (ctx.JumpRequested)
        {
            ctx.JumpRequested = false;
            // MaxMultiJumps represents the total jumps allowed. The first jump was off the ground,
            // so we allow MaxMultiJumps - 1 mid-air jumps.
            if (_midAirJumpsPerformed < (ctx.MaxMultiJumps - 1))
            {
                _midAirJumpsPerformed++;
                
                // Reset vertical velocity so upward momentum doesn't stack and multiply the jump height
                ctx.PlayerRigidbody.linearVelocity = new UnityEngine.Vector2(ctx.PlayerRigidbody.linearVelocity.x, 0f);

                ctx.PlayerRigidbody.AddForce(
                    UnityEngine.Vector2.up * ctx.JumpForce, UnityEngine.ForceMode2D.Impulse);
                ctx.PlayAnimation("isJumping");
                
                // Reset our highest peak Y since we just added new upward force
                _highestAirborneY = ctx.PlayerTransform.position.y;

                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySfx("Jump");
            }
        }

        // ── Mid-air horizontal movement ───────────────────────────────────
        float dir = ctx.InputDirection;
        if (dir != 0f && !ctx.CheckAndResetSkipMovementFrame())
        {
            ctx.PlayerTransform.Translate(
                UnityEngine.Vector3.right * dir * ctx.Speed * UnityEngine.Time.deltaTime);
            ctx.PlayerSprite.flipX = (dir < 0);
        }
    }

    private void LandPlayer()
    {
        if (ctx.EnableFallDamage)
        {
            float fallDistance = _highestAirborneY - ctx.PlayerTransform.position.y;
            if (fallDistance > ctx.MaxSafeFallHeight)
            {
                Debug.Log($"[Fall Damage] Player fell {fallDistance:F2} units (Limit: {ctx.MaxSafeFallHeight:F2}). Player died!");
                ctx.StateMachine.ChangeState(new PlayerDeadState(ctx));
                return;
            }
        }

        ctx.StateMachine.ChangeState(new PlayerIdleState(ctx));
    }

    // ── Landing detection ─────────────────────────────────────────────────

    public override void OnTriggerEnter2D(UnityEngine.Collider2D other)
    {
        if (other.CompareTag("Floor") || other.CompareTag("PlatformGround"))
        {
            // Only land if the player is falling or stationary (not moving upwards)
            if (ctx.PlayerRigidbody.linearVelocity.y <= 0.1f)
            {
                LandPlayer();
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
                        LandPlayer();
                        break;
                    }
                }
            }
        }
    }
}
