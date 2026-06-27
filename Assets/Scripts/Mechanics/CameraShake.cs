using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Shake Settings")]
    private float shakeIntensity = 0f;
    private float shakeFrequency = 10f;
    private float targetIntensity = 0f;
    private bool isShaking = false;

    [Header("Audio Settings")]
    private string currentShakeAudio = "";
    private float maxAudioVolume = 0.7f;

    [Header("Distance-Based Settings")]
    private Transform shakeSource = null;
    private float maxShakeDistance = 20f;
    private float baseIntensity = 0f;
    private Transform playerTransform = null;

    private Vector3 originalPosition;
    private CameraFollow cameraFollow;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        cameraFollow = GetComponent<CameraFollow>();
        
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    // Track max intensity for volume scaling
    private float maxShakeIntensity = 0f;

    void LateUpdate()
    {
        if (isShaking)
        {
            // Update playerTransform reference dynamically if it was missing/destroyed/respawned
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }

            // Scale targetIntensity based on proximity to player if we have a source
            if (shakeSource != null && playerTransform != null)
            {
                float distance = Vector3.Distance(playerTransform.position, shakeSource.position);
                float factor = 1f - Mathf.Clamp01(distance / maxShakeDistance);
                targetIntensity = baseIntensity * factor;
            }

            // Gradually adjust current intensity towards target
            shakeIntensity = Mathf.Lerp(shakeIntensity, targetIntensity, Time.deltaTime * 2f);

            // Update Audio Volume based on intensity
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(currentShakeAudio) && maxShakeIntensity > 0)
            {
                // Volume is proportional to current intensity / max intensity, capped by maxAudioVolume
                float volumeRatio = Mathf.Clamp01(shakeIntensity / maxShakeIntensity) * maxAudioVolume;
                AudioManager.Instance.SetLoopingSoundVolume(currentShakeAudio, volumeRatio);
            }

            // Stop shaking if intensity is very low
            if (shakeIntensity < 0.01f && targetIntensity == 0f)
            {
                StopShakeImmediate();
            }
        }
    }

    public void StartShake(float intensity, float frequency, string audioClipName, Transform source = null, float maxDistance = 20f)
    {
        // Set target intensity and base intensity
        targetIntensity = intensity;
        baseIntensity = intensity;
        shakeSource = source;
        maxShakeDistance = maxDistance;
        
        // If this is a new shake start (or higher intensity), update max for volume scaling
        if (intensity > maxShakeIntensity)
        {
            maxShakeIntensity = intensity;
        }
        
        if (!isShaking)
        {
            isShaking = true;
            currentShakeAudio = audioClipName;
            shakeFrequency = frequency;
            
            // Start shake audio (starts at low volume if we fade in, or max if we start abrupt?)
            // Logic: Start loop, let LateUpdate handle volume
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(audioClipName))
            {
                AudioManager.Instance.PlayLoopingSound(audioClipName);
                // Initialize volume
                float initialVolume = Mathf.Clamp01(shakeIntensity / maxShakeIntensity) * maxAudioVolume;
                AudioManager.Instance.SetLoopingSoundVolume(audioClipName, initialVolume);
            }
            
            // Start shake coroutine
            StartCoroutine(ShakeCoroutine());
        }
    }

    public void StopShake()
    {
        // Gradually reduce shake intensity to zero
        targetIntensity = 0f;
    }

    public void StopShakeImmediate()
    {
        isShaking = false;
        shakeIntensity = 0f;
        targetIntensity = 0f;
        maxShakeIntensity = 0f; 
        shakeSource = null;
        baseIntensity = 0f;
        CurrentShakeOffset = Vector3.zero;

        // Stop shake audio
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopLoopingSound();
        }

        currentShakeAudio = "";
    }

    // Expose the current shake offset for CameraFollow to usage
    public Vector3 CurrentShakeOffset { get; private set; }

    private IEnumerator ShakeCoroutine()
    {
        isShaking = true;

        while (isShaking)
        {
            if (shakeIntensity > 0.01f)
            {
                // Generate shake offset using Perlin noise for smooth random movement
                float x = (Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) - 0.5f) * 2f * shakeIntensity;
                float y = (Mathf.PerlinNoise(0f, Time.time * shakeFrequency) - 0.5f) * 2f * shakeIntensity;

                // Update the offset property instead of modifying transform directly
                CurrentShakeOffset = new Vector3(x, y, 0f);
            }
            else
            {
                CurrentShakeOffset = Vector3.zero;
            }

            yield return null;
        }

        CurrentShakeOffset = Vector3.zero;
    }

    public bool IsShaking()
    {
        return isShaking;
    }

    public float GetCurrentIntensity()
    {
        return shakeIntensity;
    }
}
