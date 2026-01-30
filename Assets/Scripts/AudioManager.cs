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
    public bool loop = false;

    [HideInInspector]
    public AudioSource source;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public Sound[] sounds;
    public Sound[] music;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;
    [Range(0f, 1f)]
    public float sfxVolume = 1f;
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;

    private AudioSource currentMusicSource;

    void Awake()
    {
        // Singleton pattern - ensure only one instance exists
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

    void InitializeAudio()
    {
        // Initialize sound effects
        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
        }

        // Initialize music
        foreach (Sound m in music)
        {
            m.source = gameObject.AddComponent<AudioSource>();
            m.source.clip = m.clip;
            m.source.volume = m.volume;
            m.source.pitch = m.pitch;
            m.source.loop = m.loop;
        }
    }

    // ========== SOUND EFFECTS METHODS ==========

    public void PlaySound(string name)
    {
        Sound s = System.Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + name + " not found!");
            return;
        }
        s.source.volume = s.volume * sfxVolume * masterVolume;
        s.source.Play();
    }

    public void StopSound(string name)
    {
        Sound s = System.Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + name + " not found!");
            return;
        }
        s.source.Stop();
    }

    // ========== MUSIC METHODS ==========

    public void PlayMusic(string name)
    {
        Sound m = System.Array.Find(music, track => track.name == name);
        if (m == null)
        {
            Debug.LogWarning("Music: " + name + " not found!");
            return;
        }

        // Stop current music if playing
        if (currentMusicSource != null && currentMusicSource.isPlaying)
        {
            currentMusicSource.Stop();
        }

        m.source.volume = m.volume * musicVolume * masterVolume;
        m.source.Play();
        currentMusicSource = m.source;
    }

    public void StopMusic()
    {
        if (currentMusicSource != null)
        {
            currentMusicSource.Stop();
        }
    }

    public void PauseMusic()
    {
        if (currentMusicSource != null)
        {
            currentMusicSource.Pause();
        }
    }

    public void ResumeMusic()
    {
        if (currentMusicSource != null)
        {
            currentMusicSource.UnPause();
        }
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
        if (currentMusicSource != null)
        {
            currentMusicSource.volume = currentMusicSource.volume * musicVolume * masterVolume;
        }
    }

    void UpdateAllVolumes()
    {
        foreach (Sound s in sounds)
        {
            if (s.source != null)
            {
                s.source.volume = s.volume * sfxVolume * masterVolume;
            }
        }

        if (currentMusicSource != null)
        {
            currentMusicSource.volume = currentMusicSource.volume * musicVolume * masterVolume;
        }
    }
}
