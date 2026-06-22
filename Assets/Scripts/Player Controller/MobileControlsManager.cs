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

    private bool CanSendInput()
    {
        // Block all gameplay inputs if we are currently editing the HUD layout
        if (HUDControlsEditor.Instance != null && HUDControlsEditor.Instance.IsEditMode)
        {
            return false;
        }
        return true;
    }

    public void OnMoveLeftDown()  { if (CanSendInput() && GetPlayer() != null) GetPlayer().MoveLeft(); }
    public void OnMoveLeftUp()    { if (CanSendInput() && GetPlayer() != null) GetPlayer().StopMoveLeft(); }
    
    public void OnMoveRightDown() { if (CanSendInput() && GetPlayer() != null) GetPlayer().MoveRight(); }
    public void OnMoveRightUp()   { if (CanSendInput() && GetPlayer() != null) GetPlayer().StopMoveRight(); }
    
    public void OnJumpDown()      { if (CanSendInput() && GetPlayer() != null) GetPlayer().Jump(); }
}
