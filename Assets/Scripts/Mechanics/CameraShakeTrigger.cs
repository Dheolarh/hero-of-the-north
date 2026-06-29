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

    [Header("Activation Mode")]
    [Tooltip("If true, the shake starts the moment this GameObject becomes active — no player contact needed.\n" +
             "Useful for rollers/hazards that activate via a trigger.")]
    public bool shakeOnEnable = false;

    private bool hasTriggered = false;

    // ── Activation on enable (roller just became active) ─────────────────────

    private void OnEnable()
    {
        hasTriggered = false; // Reset every time the object is enabled
        if (shakeOnEnable)
            StartCameraShake();
    }

    private void OnDisable()
    {
        hasTriggered = false;
    }

    // ── Trigger collider (Is Trigger = ON) ────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;
        if (other.CompareTag("Player"))
        {
            StartCameraShake();
            hasTriggered = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            hasTriggered = false;
    }

    // ── Solid collider (Is Trigger = OFF, e.g. a roller) ─────────────────────

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasTriggered) return;
        if (collision.collider.CompareTag("Player"))
        {
            StartCameraShake();
            hasTriggered = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
            hasTriggered = false;
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
