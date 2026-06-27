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

    [Header("Distance-Based Settings")]
    [Tooltip("If set, the shake and audio volume will dynamically scale based on how close the player is to this object. If null, falls back to Object That Stops Shake.")]
    public GameObject shakeSource;
    [Tooltip("The maximum distance from the shake source where shaking and sound are still felt.")]
    public float maxShakeDistance = 20f;

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
            GameObject source = shakeSource != null ? shakeSource : objectThatStopsShake;
            Transform sourceTransform = source != null ? source.transform : null;

            CameraShake.Instance.StartShake(shakeIntensity, shakeFrequency, shakeAudioClipName, sourceTransform, maxShakeDistance);
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
