/// <summary>
/// Abstract base for all player states.
/// Each state receives the PlayerController as its context so it can
/// read shared data (speed, flags) and call helpers (PlayAnimation, etc.).
/// </summary>
public abstract class PlayerStateBase
{
    protected PlayerController ctx;

    public PlayerStateBase(PlayerController controller)
    {
        ctx = controller;
    }

    /// <summary>Called once when transitioning INTO this state.</summary>
    public virtual void Enter() { }

    /// <summary>Called every frame while this state is active.</summary>
    public virtual void Update() { }

    /// <summary>Called once when transitioning OUT of this state.</summary>
    public virtual void Exit() { }

    // ── Collision / Trigger forwarding ─────────────────────────────────────

    /// <summary>Forwarded from PlayerController.OnTriggerEnter2D.</summary>
    public virtual void OnTriggerEnter2D(UnityEngine.Collider2D other) { }

    /// <summary>Forwarded from PlayerController.OnTriggerExit2D.</summary>
    public virtual void OnTriggerExit2D(UnityEngine.Collider2D other) { }

    /// <summary>Forwarded from PlayerController.OnCollisionEnter2D.</summary>
    public virtual void OnCollisionEnter2D(UnityEngine.Collision2D collision) { }

    /// <summary>Forwarded from PlayerController.OnCollisionExit2D.</summary>
    public virtual void OnCollisionExit2D(UnityEngine.Collision2D collision) { }
}
