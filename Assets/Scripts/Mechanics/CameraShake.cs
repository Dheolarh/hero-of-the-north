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
    }

    void LateUpdate()
    {
        if (isShaking)
        {
            // Gradually adjust current intensity towards target
            shakeIntensity = Mathf.Lerp(shakeIntensity, targetIntensity, Time.deltaTime * 2f);

            // Stop shaking if intensity is very low
            if (shakeIntensity < 0.01f && targetIntensity == 0f)
            {
                StopShakeCompletely();
            }
        }
    }

    public void StartShake(float intensity, float frequency, string audioClipName)
    {
        // Set target intensity
        targetIntensity = intensity;
        
        if (!isShaking)
        {
            isShaking = true;
            currentShakeAudio = audioClipName;
            shakeFrequency = frequency;
            
            // Start shake audio
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(audioClipName))
            {
                AudioManager.Instance.PlayLoopingSound(audioClipName);
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

    private void StopShakeCompletely()
    {
        isShaking = false;
        shakeIntensity = 0f;
        targetIntensity = 0f;

        // Stop shake audio
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopLoopingSound();
        }

        currentShakeAudio = "";
    }

    private IEnumerator ShakeCoroutine()
    {
        isShaking = true;

        while (isShaking)
        {
            if (cameraFollow != null && shakeIntensity > 0.01f)
            {
                // Generate shake offset using Perlin noise for smooth random movement
                float x = (Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) - 0.5f) * 2f * shakeIntensity;
                float y = (Mathf.PerlinNoise(0f, Time.time * shakeFrequency) - 0.5f) * 2f * shakeIntensity;

                // Apply shake offset to camera position
                transform.position += new Vector3(x, y, 0f);
            }

            yield return null;
        }
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
