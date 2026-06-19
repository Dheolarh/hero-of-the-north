public class PlayerStateMachine
{
    private PlayerStateBase _current;
    public PlayerStateBase Current => _current;
    public void ChangeState(PlayerStateBase next)
    {
        _current?.Exit();
        _current = next;
        _current?.Enter();
    }

    public void Update()
    {
        _current?.Update();
    }

    // ── Collision / Trigger forwarding ─────────────────────────────────────

    public void OnTriggerEnter2D(UnityEngine.Collider2D other)
        => _current?.OnTriggerEnter2D(other);

    public void OnTriggerExit2D(UnityEngine.Collider2D other)
        => _current?.OnTriggerExit2D(other);

    public void OnCollisionEnter2D(UnityEngine.Collision2D collision)
        => _current?.OnCollisionEnter2D(collision);

    public void OnCollisionStay2D(UnityEngine.Collision2D collision)
        => _current?.OnCollisionStay2D(collision);

    public void OnCollisionExit2D(UnityEngine.Collision2D collision)
        => _current?.OnCollisionExit2D(collision);
}
