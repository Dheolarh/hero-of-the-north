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
    public bool  IsMultiJump { get; private set; }

    public float InputDirection { get; private set; }

    public bool JumpRequested { get; set; }

    // ── UI button flags (set by the UI, read via MoveDirection) ───────────
    private bool _uiMoveLeft;
    private bool _uiMoveRight;

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

        if (LevelManager.Instance != null)
            IsMultiJump = LevelManager.Instance.CurrentLevelData?.allowMultiJumps ?? false;

        // Boot the state machine into Idle
        StateMachine = new PlayerStateMachine();
        StateMachine.ChangeState(new PlayerIdleState(this));
    }

    void Update()
    {
        if (GameManager.Instance.isGameOver) return;

        // Compute shared input once per frame so states can read it
        InputDirection = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)  || _uiMoveLeft)  InputDirection -= 1f;
        if (Input.GetKey(KeyCode.RightArrow) || _uiMoveRight) InputDirection += 1f;

        if (Input.GetKeyDown(KeyCode.Space)) JumpRequested = true;

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
