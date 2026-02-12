using UnityEngine;
using TMPro;
using System.Collections;

public class TextModifier : MonoBehaviour
{
    public enum TextStyle
    {
        None,
        Typewriter,
        Pulsating
    }

    [Header("Configuration")]
    [SerializeField] private TMP_Text targetText;
    public TextStyle currentStyle = TextStyle.None;

    [Header("Typewriter Settings")]
    [Tooltip("Characters per second (Higher = Faster)")]
    public float typingSpeed = 20f; 
    [Tooltip("If true, starts typing automatically when object is enabled")]
    public bool playOnEnable = true;

    [Header("Pulsating Settings")]
    public float pulseSpeed = 1f;
    public float minScale = 0.9f;
    public float maxScale = 1.1f;
    
    // Store original text content for the typewriter effect
    private string originalText;
    private Coroutine currentCoroutine;

    void Awake()
    {
        // Auto-assign if not set manually
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();
        
        if (targetText != null)
            originalText = targetText.text;
    }

    void OnEnable()
    {
        if (targetText != null)
        {
            // Ensure we have the latest text if it changed while disabled
            // But if text is modified externally, we might want to preserve that content?
            // For now, let's just make sure we capture it.
             if (string.IsNullOrEmpty(originalText)) originalText = targetText.text;
        }

        if (playOnEnable)
        {
            ApplyStyle();
        }
    }

    /// <summary>
    /// Apply the selected style. Call this if you change the style at runtime.
    /// </summary>
    public void ApplyStyle()
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);
        
        // Reset state
        if (targetText != null)
        {
            targetText.transform.localScale = Vector3.one;
            // Restore full text if switching away from typewriter mid-type
            targetText.text = originalText; 
        }

        switch (currentStyle)
        {
            case TextStyle.Typewriter:
                currentCoroutine = StartCoroutine(TypewriterEffect());
                break;
            case TextStyle.Pulsating:
                currentCoroutine = StartCoroutine(PulsatingEffect());
                break;
            case TextStyle.None:
                break;
        }
    }

    /// <summary>
    /// Sets new text content and restarts the current effect.
    /// </summary>
    public void SetText(string content)
    {
        originalText = content;
        if (targetText != null)
        {
            targetText.text = content;
            ApplyStyle(); 
        }
    }

    private IEnumerator TypewriterEffect()
    {
        if (targetText == null) yield break;

        // Ensure originalText is valid
        if (string.IsNullOrEmpty(originalText)) originalText = targetText.text;

        targetText.text = ""; // Clear text
        
        // Calculate delay based on characters per second
        // Prevent division by zero
        float delay = 1f / Mathf.Max(0.1f, typingSpeed);

        foreach (char c in originalText)
        {
            targetText.text += c;
            yield return new WaitForSeconds(delay);
        }
    }

    private IEnumerator PulsatingEffect()
    {
        if (targetText == null) yield break;

        // Ensure text is visible
        targetText.text = originalText;

        while (true)
        {
            // PingPong returns value between 0 and 1
            float t = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            float scale = Mathf.Lerp(minScale, maxScale, t);
            
            targetText.transform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
    }
}
