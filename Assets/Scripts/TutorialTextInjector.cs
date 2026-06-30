using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class TutorialTextInjector : MonoBehaviour
{
    [Header("Target Text (auto-assigned if blank)")]
    [SerializeField] private TMP_Text targetText;

    [Header("Message")]
    [Tooltip("The full message to display. Use {username} where the player name should appear.\n\nLeave the TMP text component blank in the scene — this script sets it at runtime.")]
    [TextArea(8, 30)]
    [SerializeField] private string messageTemplate =
        "Dear {username},\n" +
        "I write to you with sorrow in my heart. Dahak has once again struck our kingdom, capturing our allies and leaving the survivors in hiding. They have called upon your strength to venture into the cold and rescue those we have lost.\n\n" +
        "Dahak is exceedingly dangerous. He has set treacherous traps along the frozen paths, so you must tread with the utmost caution.\n\n" +
        "Troy, our last brave adventurer, has set out before you to scout the wilderness and clear a safe path. However, the blizzards are fierce and the terrain is hostile; it takes Troy time to navigate the snow and find a safe passage. Do not rush, as he can only secure one new passage each day.\n\n" +
        "The spirits of the North watch your deeds closely, and your honor will grow for every companion you return to safety. Time is of the essence; the faster you navigate the frozen wastes, the greater your renown. Yet, guard your strength well. Every time you fall and you will be punished, your standing will diminish, as we value swift and decisive triumphs.\n\n" +
        "May the light guide your steps...";

    [Header("Placeholder")]
    [Tooltip("The placeholder token to replace with the player's username.")]
    [SerializeField] private string placeholder = "{username}";

    [Tooltip("Fallback shown when the username is not yet available.")]
    [SerializeField] private string fallback = "hero";

    // Raw template text (captured once in Awake so we can re-inject cleanly)
    private string template;

    void Awake()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();

        // Prefer the inspector TextArea field as the template source.
        // Fall back to whatever is in the TMP component if the field was left blank.
        if (!string.IsNullOrEmpty(messageTemplate))
            template = messageTemplate;
        else if (targetText != null)
            template = targetText.text;
    }

    void OnEnable()
    {
        // If DevvitBridge already has a username (fetched early in its Awake),
        // inject immediately — no waiting needed.
        if (DevvitBridge.Instance != null &&
            !string.IsNullOrWhiteSpace(DevvitBridge.Instance.username))
        {
            Inject(DevvitBridge.Instance.username);
            return;
        }

        // Otherwise subscribe to the event so we react the instant it arrives.
        // Also inject the fallback now so the text isn't blank while waiting.
        Inject(fallback);

        if (DevvitBridge.Instance != null)
        {
            DevvitBridge.Instance.OnUsernameReady += OnUsernameReady;
        }
        else
        {
            // DevvitBridge not present yet — poll until it appears, then hook the event
            StartCoroutine(WaitForBridge());
        }
    }

    void OnDisable()
    {
        // Always unsubscribe to prevent memory leaks / duplicate calls
        if (DevvitBridge.Instance != null)
            DevvitBridge.Instance.OnUsernameReady -= OnUsernameReady;
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private void OnUsernameReady(string name)
    {
        Inject(name);
        // Unsubscribe — we only need this once per panel open
        if (DevvitBridge.Instance != null)
            DevvitBridge.Instance.OnUsernameReady -= OnUsernameReady;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void Inject(string name)
    {
        if (targetText == null) return;

        // Defensive check: if template is empty, fetch it now
        if (string.IsNullOrEmpty(template))
        {
            if (!string.IsNullOrEmpty(messageTemplate))
                template = messageTemplate;
            else
                template = targetText.text;
        }

        if (string.IsNullOrEmpty(template)) return;

        string cleanName = DevvitBridge.TrimUsername(name, fallback);
        string newText = template;

        // 1. Try replacing with the Inspector-defined placeholder (case-insensitive)
        if (!string.IsNullOrEmpty(placeholder))
        {
            newText = ReplaceCaseInsensitive(newText, placeholder, cleanName);
        }

        // 2. Unconditionally replace standard bracket placeholders to be absolutely bulletproof
        newText = ReplaceCaseInsensitive(newText, "{username}", cleanName);
        newText = ReplaceCaseInsensitive(newText, "{Username}", cleanName);
        newText = ReplaceCaseInsensitive(newText, "{USERNAME}", cleanName);
        newText = ReplaceCaseInsensitive(newText, "{name}", cleanName);
        newText = ReplaceCaseInsensitive(newText, "{Name}", cleanName);
        
        // 3. If the template contains "Dear hero", also replace "hero"/"Hero"
        // but only if we got a real name (to avoid infinite loop of replacing "hero" with "hero")
        if (cleanName != fallback)
        {
            newText = ReplaceCaseInsensitive(newText, "hero", cleanName);
            newText = ReplaceCaseInsensitive(newText, "Hero", cleanName);
        }

        targetText.text = newText;

        // Re-run TextModifier effect (typewriter, etc.) with the updated text
        TextModifier modifier = GetComponent<TextModifier>();
        if (modifier != null)
            modifier.SetText(targetText.text);
    }

    private string ReplaceCaseInsensitive(string str, string oldVal, string newVal)
    {
        if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(oldVal)) return str;
        
        int index = str.IndexOf(oldVal, StringComparison.OrdinalIgnoreCase);
        while (index != -1)
        {
            str = str.Substring(0, index) + newVal + str.Substring(index + oldVal.Length);
            index = str.IndexOf(oldVal, index + newVal.Length, StringComparison.OrdinalIgnoreCase);
        }
        return str;
    }

    private IEnumerator WaitForBridge()
    {
        float elapsed = 0f;
        float timeout = 10f;

        while (elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;

            if (DevvitBridge.Instance != null)
            {
                // If username already arrived, inject now
                if (!string.IsNullOrWhiteSpace(DevvitBridge.Instance.username))
                {
                    Inject(DevvitBridge.Instance.username);
                    yield break;
                }

                // Otherwise hook the event and let it do the rest
                DevvitBridge.Instance.OnUsernameReady += OnUsernameReady;
                yield break;
            }
        }

        Debug.LogWarning("[TutorialTextInjector] DevvitBridge not found after 10 s — keeping fallback text.");
    }
}
