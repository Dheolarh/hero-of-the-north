using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // ── Inspector fields ───────────────────────────────────────────────────
    [SerializeField] float speed;
    [SerializeField] float jumpForce;
    [SerializeField] bool enableFallDamage = false;
    [SerializeField] float maxSafeFallHeight = 20f;

    // ── Shared component references (read by states) ───────────────────────
    public Transform     PlayerTransform  { get; private set; }
    public Rigidbody2D   PlayerRigidbody  { get; private set; }
    public SpriteRenderer PlayerSprite    { get; private set; }
    public Animator      PlayerAnimation  { get; private set; }
    public CameraFollow  CameraFollow     { get; private set; }

    // ── Shared data properties (read / written by states) ─────────────────
    public float Speed     { get => speed; set => speed = value; }
    public float JumpForce { get => jumpForce; set => jumpForce = value; }
    public bool  EnableFallDamage { get => enableFallDamage; set => enableFallDamage = value; }
    public float MaxSafeFallHeight => maxSafeFallHeight;
    public int   MaxMultiJumps { get; set; }

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

        if (LevelCreatorUI.Instance != null && LevelCreatorUI.Instance.IsPlaytesting)
        {
            MaxMultiJumps = LevelCreatorUI.Instance.playerMaxJumps;
            Speed = LevelCreatorUI.Instance.playerMoveSpeed;
            JumpForce = LevelCreatorUI.Instance.playerJumpForce;
            EnableFallDamage = LevelCreatorUI.Instance.playerEnableFallDamage;
        }
        else if (LevelManager.Instance != null)
        {
            MaxMultiJumps = LevelManager.Instance.CurrentLevelData?.multiJumpCount ?? 1;
        }
        else
        {
            MaxMultiJumps = 1; // Default to normal jump in editor/sandbox scenes
        }

        // Boot the state machine into Idle
        StateMachine = new PlayerStateMachine();
        StateMachine.ChangeState(new PlayerIdleState(this));
    }

    void Update()
    {
        bool isPlaytesting = LevelCreatorUI.Instance != null && LevelCreatorUI.Instance.IsPlaytesting;
        if (GameManager.Instance == null && !isPlaytesting) return;

        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;
        if (GameManager.Instance != null && GameManager.Instance.isLevelCompleted)
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
            // Keyboard / on-screen buttons
            if (Input.GetKey(KeyCode.LeftArrow)  || Input.GetKey(KeyCode.A) || _uiMoveLeft)  InputDirection -= 1f;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D) || _uiMoveRight) InputDirection += 1f;

            // Controller: left stick & D-pad (Unity's built-in "Horizontal" axis covers both)
            float controllerAxis = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(controllerAxis) > 0.1f)
                InputDirection = Mathf.Clamp(InputDirection + controllerAxis, -1f, 1f);

            // Jump: keyboard + controller South button (A on Xbox / Cross on PS)
            if (Input.GetKeyDown(KeyCode.Space)  || Input.GetKeyDown(KeyCode.W)      ||
                Input.GetKeyDown(KeyCode.UpArrow) ||
                Input.GetKeyDown(KeyCode.JoystickButton0))   // A / Cross
                JumpRequested = true;
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

    private bool _skipMovementFrame = false;

    public void SkipMovementFrame()
    {
        _skipMovementFrame = true;
    }

    public bool CheckAndResetSkipMovementFrame()
    {
        if (_skipMovementFrame)
        {
            _skipMovementFrame = false;
            return true;
        }
        return false;
    }
}
