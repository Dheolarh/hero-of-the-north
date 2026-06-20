using UnityEngine;

/// <summary>
/// A bridge script to allow permanent UI buttons in the Game scene 
/// to control the dynamically spawned player inside level prefabs.
/// </summary>
public class MobileControlsManager : MonoBehaviour
{
    private PlayerController _player;

    private PlayerController GetPlayer()
    {
        // Dynamically find the player if it hasn't been cached yet,
        // or if the previous player was destroyed (e.g. changing levels).
        if (_player == null)
        {
            _player = FindFirstObjectByType<PlayerController>();
        }
        return _player;
    }

    public void OnMoveLeftDown()  { if (GetPlayer() != null) GetPlayer().MoveLeft(); }
    public void OnMoveLeftUp()    { if (GetPlayer() != null) GetPlayer().StopMoveLeft(); }
    
    public void OnMoveRightDown() { if (GetPlayer() != null) GetPlayer().MoveRight(); }
    public void OnMoveRightUp()   { if (GetPlayer() != null) GetPlayer().StopMoveRight(); }
    
    public void OnJumpDown()      { if (GetPlayer() != null) GetPlayer().Jump(); }
}
