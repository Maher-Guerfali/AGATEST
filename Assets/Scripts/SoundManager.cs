using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource musicSource;

    [Header("Sound Effects")]
    public AudioClip flipSound;
    public AudioClip matchSound;
    public AudioClip mismatchSound;
    public AudioClip gameOverSound;

    [Header("Settings")]
    [Range(0f, 1f)] public float sfxVolume = 0.7f;
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    public bool soundEnabled = true;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudioSources()
    {
      

        UpdateVolumes();
    }

    private void LoadSettings()
    {
        soundEnabled = PlayerPrefs.GetInt("SoundEnabled", 1) == 1;
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.7f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        UpdateVolumes();
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetInt("SoundEnabled", soundEnabled ? 1 : 0);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.Save();
    }

    #region Public Sound Methods

    public void PlayFlip()
    {
        PlaySFX(flipSound);
    }

    public void PlayMatch()
    {
        PlaySFX(matchSound);
    }

    public void PlayMismatch()
    {
        PlaySFX(mismatchSound);
    }

    public void PlayGameOver()
    {
        PlaySFX(gameOverSound);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (!soundEnabled || sfxSource == null || clip == null) return;

        sfxSource.PlayOneShot(clip);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (!soundEnabled || musicSource == null || clip == null) return;

        if (musicSource.clip != clip)
        {
            musicSource.clip = clip;
            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    #endregion

    #region Settings Control

    public void ToggleSound()
    {
        soundEnabled = !soundEnabled;
        UpdateVolumes();
        SaveSettings();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
        SaveSettings();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
        SaveSettings();
    }

    private void UpdateVolumes()
    {
        if (sfxSource != null)
            sfxSource.volume = soundEnabled ? sfxVolume : 0f;

        if (musicSource != null)
            musicSource.volume = soundEnabled ? musicVolume : 0f;
    }

    #endregion

    #region Editor Utilities

#if UNITY_EDITOR
    [ContextMenu("Test Flip Sound")]
    private void TestFlipSound()
    {
        PlayFlip();
    }

    [ContextMenu("Test Match Sound")]
    private void TestMatchSound()
    {
        PlayMatch();
    }

    [ContextMenu("Test Mismatch Sound")]
    private void TestMismatchSound()
    {
        PlayMismatch();
    }

    [ContextMenu("Test GameOver Sound")]
    private void TestGameOverSound()
    {
        PlayGameOver();
    }
#endif

    #endregion
}