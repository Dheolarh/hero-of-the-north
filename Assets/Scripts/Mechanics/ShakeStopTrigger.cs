using UnityEngine;

public class ShakeStopTrigger : MonoBehaviour
{
    [Header("Stop Shake Settings")]
    [Tooltip("The GameObject that should trigger the shake stop when it collides with this trigger")]
    public GameObject objectThatStopsShake;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the colliding object is the one that should stop the shake
        if (objectThatStopsShake != null && other.gameObject == objectThatStopsShake)
        {
            StopCameraShake();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Also support collision-based triggers
        if (objectThatStopsShake != null && collision.gameObject == objectThatStopsShake)
        {
            StopCameraShake();
        }
    }

    private void StopCameraShake()
    {
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.StopShake();
            Debug.Log("[ShakeStopTrigger] Camera shake stop triggered!");
        }
    }
}
