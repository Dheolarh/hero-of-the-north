using UnityEngine;

public class CameraShakeTrigger : MonoBehaviour
{
    [Header("Shake Settings")]
    public float shakeIntensity = 0.3f;
    public float shakeFrequency = 10f;
    public string shakeAudioClipName = "Earthquake"; // Default or specific clip

    [Header("Stop Logic")]
    public GameObject stopShakeTrigger;
    public GameObject objectThatStopsShake;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            StartCameraShake();
            hasTriggered = true; // Prevent re-triggering? Or allow multiple? Usually once.
        }
    }

    private void StartCameraShake()
    {
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.StartShake(shakeIntensity, shakeFrequency, shakeAudioClipName);
            Debug.Log($"[CameraShakeTrigger] Shake started via Trigger Zone: {gameObject.name}");

            // Configure Stop Trigger if provided
            if (stopShakeTrigger != null && objectThatStopsShake != null)
            {
                ShakeStopTrigger stopTriggerScript = stopShakeTrigger.GetComponent<ShakeStopTrigger>();
                if (stopTriggerScript == null)
                {
                    stopTriggerScript = stopShakeTrigger.AddComponent<ShakeStopTrigger>();
                }
                
                stopTriggerScript.objectThatStopsShake = objectThatStopsShake;
                Debug.Log($"[CameraShakeTrigger] Stop Trigger configured on {stopShakeTrigger.name} to wait for {objectThatStopsShake.name}");
            }
        }
        else
        {
            Debug.LogError("[CameraShakeTrigger] CameraShake.Instance is null! Ensure CameraShake is in the scene.");
        }
    }
}
