using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // ── Inspector fields ───────────────────────────────────────────────────
    [SerializeField] float speed;
    [SerializeField] float jumpForce;

    // ── Shared component references (read by states) ───────────────────────
    public Transform     PlayerTransform  { get; private set; }
    public Rigidbody2D   PlayerRigidbody  { get; private set; }
    public SpriteRenderer PlayerSprite    { get; private set; }
    public Animator      PlayerAnimation  { get; private set; }
    public CameraFollow  CameraFollow     { get; private set; }

    // ── Shared data properties (read / written by states) ─────────────────
    public float Speed     => speed;
    public float JumpForce => jumpForce;
    public int   MaxMultiJumps { get; private set; }

    public float InputDirection { get; private set; }

    public bool JumpRequested { get; set; }

    // ── UI button flags (set by the UI, read via MoveDirection) ───────────
    private bool _uiMoveLeft;
    private bool _uiMoveRight;

    // Tracks whether we've already applied the level-complete freeze
    private bool _levelCompleteFrozen = false;

    // Cached collider — disabled on level complete so death objects pass through
    private Collider2D _playerCollider;

    // ── State machine ──────────────────────────────────────────────────────
    public PlayerStateMachine StateMachine { get; private set; }

    // ── Unity lifecycle ────────────────────────────────────────────────────

    void Start()
    {
        PlayerTransform  = GetComponent<Transform>();
        PlayerRigidbody  = GetComponent<Rigidbody2D>();
        PlayerSprite     = GetComponent<SpriteRenderer>();
        PlayerAnimation  = GetComponent<Animator>();
        CameraFollow     = Camera.main.GetComponent<CameraFollow>();
        _playerCollider  = GetComponent<Collider2D>();

        if (LevelManager.Instance != null)
            MaxMultiJumps = LevelManager.Instance.CurrentLevelData?.multiJumpCount ?? 0;

        // Boot the state machine into Idle
        StateMachine = new PlayerStateMachine();
        StateMachine.ChangeState(new PlayerIdleState(this));
    }

    void Update()
    {
        if (GameManager.Instance.isGameOver) return;
        if (GameManager.Instance.isLevelCompleted)
        {
            if (!_levelCompleteFrozen)
            {
                _levelCompleteFrozen = true;

                // Switch to idle animation
                PlayAnimation("isIdle");

                // Stop all audio (walking sound etc.)
                if (AudioManager.Instance != null)
                    AudioManager.Instance.StopWalkingSound();

                // Disable collider so death objects (spikes, traps) pass through
                if (_playerCollider != null)
                    _playerCollider.enabled = false;
            }

            // Zero velocity and input every frame so gravity/momentum doesn't move them
            PlayerRigidbody.linearVelocity = Vector2.zero;
            PlayerRigidbody.gravityScale   = 0f;
            InputDirection = 0f;
            JumpRequested  = false;
            return;
        }

        // Compute shared input once per frame so states can read it
        InputDirection = 0f;
        if (HUDControlsEditor.Instance == null || !HUDControlsEditor.Instance.IsEditMode)
        {
            if (Input.GetKey(KeyCode.LeftArrow)  || Input.GetKey(KeyCode.A) || _uiMoveLeft)  InputDirection -= 1f;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D) || _uiMoveRight) InputDirection += 1f;

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) JumpRequested = true;
        }
        else
        {
            // Reset any UI button flags if we entered edit mode while dragging
            _uiMoveLeft = false;
            _uiMoveRight = false;
            JumpRequested = false;
        }

        StateMachine.Update();
    }

    // ── Public helpers called by state classes ─────────────────────────────

    public void PlayAnimation(string animationName)
    {
        PlayerAnimation.SetBool("isWalking", false);
        PlayerAnimation.SetBool("isJumping", false);
        PlayerAnimation.SetBool("isIdle",    false);
        PlayerAnimation.SetBool("isDead",    false);
        PlayerAnimation.SetBool(animationName, true);
    }

    public void ResetPlayerRotation()
    {
        if (PlayerTransform.rotation != Quaternion.identity)
            PlayerTransform.rotation = Quaternion.identity;
    }

    public bool IsGrounded()
    {
        if (_playerCollider == null) return false;

        Bounds bounds = _playerCollider.bounds;
        Vector2 checkPosition = new Vector2(bounds.center.x, bounds.min.y + 0.05f);
        float checkRadius = 0.15f;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(checkPosition, checkRadius);
        for (int i = 0; i < colliders.Length; i++)
        {
            var col = colliders[i];
            if (col != null && col != _playerCollider)
            {
                if (col.CompareTag("Floor") || col.CompareTag("PlatformGround"))
                {
                    return true;
                }
            }
        }
        return false;
    }

    // ── UI button API (unchanged — wired to UI buttons in the Inspector) ───

    public void MoveLeft()       => _uiMoveLeft  = true;
    public void StopMoveLeft()   => _uiMoveLeft  = false;
    public void MoveRight()      => _uiMoveRight = true;
    public void StopMoveRight()  => _uiMoveRight = false;

    public void Jump() => JumpRequested = true;

    // ── Physics callbacks — forwarded straight to the active state ─────────

    private void OnTriggerEnter2D(Collider2D other)
        => StateMachine.OnTriggerEnter2D(other);

    private void OnTriggerExit2D(Collider2D other)
        => StateMachine.OnTriggerExit2D(other);

    private void OnCollisionEnter2D(Collision2D collision)
        => StateMachine.OnCollisionEnter2D(collision);

    private void OnCollisionStay2D(Collision2D collision)
        => StateMachine.OnCollisionStay2D(collision);

    private void OnCollisionExit2D(Collision2D collision)
        => StateMachine.OnCollisionExit2D(collision);

    // ── Coroutine helpers (called by states via ctx.StartCoroutine) ────────

    public Coroutine StartStopCameraCoroutine(float delay)
        => StartCoroutine(StopCameraAfterDelay(delay));

    private IEnumerator StopCameraAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (CameraFollow != null)
            CameraFollow.StopFollowing();
    }
}
