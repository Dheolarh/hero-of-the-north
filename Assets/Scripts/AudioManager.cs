using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    [Range(0f, 1f)]
    public float volume = 0.7f;
    [Range(0.1f, 3f)]
    public float pitch = 1f;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Source Pools")]
    private List<AudioSource> sfxSourcePool = new List<AudioSource>();
    private List<AudioSource> loopingSourcePool = new List<AudioSource>();
    private AudioSource bgMusicSource;
    private AudioSource walkingSource;  // Dedicated source for player walking

    [Header("One-Shot Sound Effects")]
    public Sound[] oneShotSounds;

    [Header("Walking Sound (Looping)")]
    public Sound[] walkingSounds;  // Only for player walking

    [Header("Trigger Looping Sounds")]
    public Sound[] triggerLoopingSounds;  // Camera shake, danger zones, etc.

    [Header("Background Music")]
    public Sound[] backgroundMusic;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;
    [Range(0f, 1f)]
    public float sfxVolume = 1f;
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;

    private Sound currentMusic;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudio();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        PlayMusic("Background");
    }

    void InitializeAudio()
    {
        // Background music source
        bgMusicSource = gameObject.AddComponent<AudioSource>();
        bgMusicSource.loop = true;
        bgMusicSource.playOnAwake = false;

        // Create 2 initial one-shot SFX sources
        for (int i = 0; i < 2; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.loop = false;
            source.playOnAwake = false;
            sfxSourcePool.Add(source);
        }

        // Create 2 initial looping SFX sources
        for (int i = 0; i < 2; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            loopingSourcePool.Add(source);
        }

        // Create dedicated walking source
        walkingSource = gameObject.AddComponent<AudioSource>();
        walkingSource.loop = true;
        walkingSource.playOnAwake = false;
    }

    // ========== ONE-SHOT SOUND EFFECTS ==========

    public void PlaySfx(string soundName)
    {
        Sound s = System.Array.Find(oneShotSounds, sound => sound.name == soundName);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + soundName + " not found in oneShotSounds!");
            return;
        }

        // Find an available source from the pool
        AudioSource availableSource = GetAvailableSfxSource();
        
        availableSource.pitch = s.pitch;
        availableSource.PlayOneShot(s.clip, s.volume * sfxVolume * masterVolume);
    }

    private AudioSource GetAvailableSfxSource()
    {
        // Check existing sources for availability
        foreach (AudioSource source in sfxSourcePool)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }

        // All sources are busy, create a temporary one
        AudioSource tempSource = gameObject.AddComponent<AudioSource>();
        tempSource.loop = false;
        tempSource.playOnAwake = false;
        sfxSourcePool.Add(tempSource);
        
        Debug.Log($"[AudioManager] Created extra SFX source. Pool size: {sfxSourcePool.Count}");
        
        // Clean up extra sources after a delay
        StartCoroutine(CleanupExtraSfxSource(tempSource));
        
        return tempSource;
    }

    private IEnumerator CleanupExtraSfxSource(AudioSource source)
    {
        // Wait for the source to finish playing
        yield return new WaitForSeconds(5f); // Wait a bit to allow sound to finish
        
        // Only remove if we have more than 2 sources and this one is not playing
        if (sfxSourcePool.Count > 2 && !source.isPlaying)
        {
            sfxSourcePool.Remove(source);
            Destroy(source);
            Debug.Log($"[AudioManager] Removed extra SFX source. Pool size: {sfxSourcePool.Count}");
        }
    }

    // Public method for Unity UI button events
    public void PlayButtonClickSound()
    {
        PlaySfx("ButtonClick");
    }

    // ========== LOOPING SOUND EFFECTS ==========

    public void PlayLoopingSound(string soundName)
    {
        Sound s = System.Array.Find(triggerLoopingSounds, sound => sound.name == soundName);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + soundName + " not found in triggerLoopingSounds!");
            return;
        }

        // Find an available source from the pool
        AudioSource availableSource = GetAvailableLoopingSource();
        
        availableSource.clip = s.clip;
        availableSource.volume = s.volume * sfxVolume * masterVolume;
        availableSource.pitch = s.pitch;
        availableSource.Play();
    }

    private AudioSource GetAvailableLoopingSource()
    {
        // Check existing sources for availability
        foreach (AudioSource source in loopingSourcePool)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }

        // All sources are busy, create a temporary one
        AudioSource tempSource = gameObject.AddComponent<AudioSource>();
        tempSource.loop = true;
        tempSource.playOnAwake = false;
        loopingSourcePool.Add(tempSource);
        
        Debug.Log($"[AudioManager] Created extra looping source. Pool size: {loopingSourcePool.Count}");
        
        return tempSource;
    }

    public void StopLoopingSound()
    {
        // Stop all looping sounds and clean up extras
        for (int i = loopingSourcePool.Count - 1; i >= 0; i--)
        {
            AudioSource source = loopingSourcePool[i];
            source.Stop();
            
            // Remove extra sources (keep only the first 2)
            if (i >= 2)
            {
                loopingSourcePool.RemoveAt(i);
                Destroy(source);
                Debug.Log($"[AudioManager] Removed extra looping source. Pool size: {loopingSourcePool.Count}");
            }
        }
    }

    public void StopSpecificLoopingSound(string soundName)
    {
        Sound s = System.Array.Find(triggerLoopingSounds, sound => sound.name == soundName);
        if (s == null) return;

        // Find and stop the specific sound
        for (int i = loopingSourcePool.Count - 1; i >= 0; i--)
        {
            AudioSource source = loopingSourcePool[i];
            if (source.clip == s.clip && source.isPlaying)
            {
                source.Stop();
                
                // Remove if it's an extra source
                if (i >= 2)
                {
                    loopingSourcePool.RemoveAt(i);
                    Destroy(source);
                    Debug.Log($"[AudioManager] Removed extra looping source. Pool size: {loopingSourcePool.Count}");
                }
            }
        }
    }

    public void SetLoopingSoundVolume(string soundName, float volume)
    {
        Sound s = System.Array.Find(triggerLoopingSounds, sound => sound.name == soundName);
        if (s == null) return;

        // Find the source playing this sound and adjust volume
        foreach (AudioSource source in loopingSourcePool)
        {
            if (source.clip == s.clip && source.isPlaying)
            {
                // Calculate final volume based on Master and SFX settings
                float finalVolume = Mathf.Clamp01(volume) * sfxVolume * masterVolume;
                source.volume = finalVolume;
            }
        }
    }

    // ========== WALKING SOUND (DEDICATED) ==========

    public void PlayWalkingSound()
    {
        Sound s = System.Array.Find(walkingSounds, sound => sound.name == "Walk");
        if (s == null)
        {
            Debug.LogWarning("Sound: Walk not found in walkingSounds!");
            return;
        }

        // Only play if not already playing
        if (!walkingSource.isPlaying)
        {
            walkingSource.clip = s.clip;
            walkingSource.volume = s.volume * sfxVolume * masterVolume;
            walkingSource.pitch = s.pitch;
            walkingSource.Play();
        }
    }

    public void StopWalkingSound()
    {
        walkingSource.Stop();
    }

    public void StopAllSoundsExceptMusic()
    {
        // Stop walking sound
        walkingSource.Stop();

        // Stop all one-shot SFX sources
        foreach (AudioSource source in sfxSourcePool)
        {
            source.Stop();
        }

        // Stop all looping trigger sounds
        for (int i = loopingSourcePool.Count - 1; i >= 0; i--)
        {
            AudioSource source = loopingSourcePool[i];
            source.Stop();
            
            // Remove extra sources
            if (i >= 2)
            {
                loopingSourcePool.RemoveAt(i);
                Destroy(source);
            }
        }

        Debug.Log("[AudioManager] Stopped all sounds except background music");
    }

    // ========== BACKGROUND MUSIC ==========

    public void PlayMusic(string name)
    {
        Sound m = System.Array.Find(backgroundMusic, track => track.name == name);
        if (m == null)
        {
            Debug.LogWarning("Music: " + name + " not found!");
            return;
        }

        // Stop current music if playing
        if (bgMusicSource.isPlaying)
        {
            bgMusicSource.Stop();
        }

        currentMusic = m;
        bgMusicSource.clip = m.clip;
        bgMusicSource.volume = m.volume * musicVolume * masterVolume;
        bgMusicSource.pitch = m.pitch;
        bgMusicSource.loop = true;
        bgMusicSource.Play();
    }

    public void StopMusic()
    {
        bgMusicSource.Stop();
    }

    public void PauseMusic()
    {
        bgMusicSource.Pause();
    }

    public void ResumeMusic()
    {
        bgMusicSource.UnPause();
    }

    // ========== VOLUME CONTROL ==========

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (currentMusic != null && bgMusicSource.isPlaying)
        {
            bgMusicSource.volume = currentMusic.volume * musicVolume * masterVolume;
        }
    }

    void UpdateAllVolumes()
    {
        if (currentMusic != null && bgMusicSource.isPlaying)
        {
            bgMusicSource.volume = currentMusic.volume * musicVolume * masterVolume;
        }
    }
}
