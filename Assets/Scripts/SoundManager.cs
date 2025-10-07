using UnityEngine;

/// <summary>
/// Central audio management system handling all sound effects and music for the game.
/// Implements Singleton pattern with DontDestroyOnLoad to persist across scenes.
/// 
/// RESPONSIBILITIES:
/// - Play sound effects (flip, match, mismatch, game over)
/// - Manage background music playback
/// - Control volume levels (SFX and Music separately)
/// - Toggle sound on/off
/// - Persist audio settings using PlayerPrefs
/// - Provide global access point for audio playback
/// 
/// AUDIO ARCHITECTURE:
/// - Uses 2 AudioSource components:
///   1. sfxSource: For one-shot sound effects (flip, match, etc.)
///   2. musicSource: For looping background music
/// 
/// PERSISTENCE:
/// - Settings saved to PlayerPrefs:
///   * SoundEnabled (0 or 1)
///   * SFXVolume (0.0 to 1.0)
///   * MusicVolume (0.0 to 1.0)
/// - Automatically loads on game start
/// - Automatically saves when changed
/// 
/// USAGE:
/// SoundManager.Instance.PlayMatch();
/// SoundManager.Instance.SetSFXVolume(0.8f);
/// 
/// Author: [Your Team Name]
/// Last Modified: 2025
/// </summary>
public class SoundManager : MonoBehaviour
{
    #region Singleton Pattern

    /// <summary>
    /// Global access point to the SoundManager instance.
    /// Use: SoundManager.Instance.PlayFlip();
    /// </summary>
    public static SoundManager Instance { get; private set; }

    #endregion

    #region Inspector References

    [Header("Audio Sources")]
    [Tooltip("AudioSource for sound effects (flip, match, mismatch). Uses PlayOneShot for overlapping sounds.")]
    public AudioSource sfxSource;

    [Tooltip("AudioSource for background music. Loops continuously.")]
    public AudioSource musicSource;

    [Header("Sound Effects")]
    [Tooltip("Sound played when card is flipped")]
    public AudioClip flipSound;

    [Tooltip("Sound played when cards match successfully")]
    public AudioClip matchSound;

    [Tooltip("Sound played when cards don't match")]
    public AudioClip mismatchSound;

    [Tooltip("Sound played when all cards are matched (game complete)")]
    public AudioClip gameOverSound;

    [Header("Settings")]
    [Tooltip("Volume level for sound effects (0 = silent, 1 = full volume)")]
    [Range(0f, 1f)] public float sfxVolume = 0.7f;

    [Tooltip("Volume level for background music (0 = silent, 1 = full volume)")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;

    [Tooltip("Master toggle for all audio (true = enabled, false = muted)")]
    public bool soundEnabled = true;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes Singleton and ensures persistence across scene loads.
    /// 
    /// SINGLETON PATTERN:
    /// - First instance: Becomes THE instance and persists
    /// - Duplicate instances: Destroyed immediately
    /// 
    /// PERSISTENCE:
    /// - DontDestroyOnLoad ensures SoundManager survives scene transitions
    /// - Useful for menu → game → menu transitions
    /// </summary>
    private void Awake()
    {
        // Singleton pattern with persistence
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
            LoadSettings();
            Debug.Log("[SoundManager] Instance created and persisted across scenes");
        }
        else
        {
            // CRITICAL FIX: Stop ALL audio sources on the duplicate before destroying
            if (sfxSource != null)
            {
                sfxSource.Stop();
                sfxSource.enabled = false;
            }
            if (musicSource != null)
            {
                musicSource.Stop();
                musicSource.enabled = false;
            }

            // Destroy immediately to prevent any audio from playing
            DestroyImmediate(gameObject);
            Debug.Log("[SoundManager] Duplicate instance destroyed immediately (all audio stopped)");
            return;
        }
    }
    /// <summary>
    /// Initializes audio source components and applies initial volume settings.
    /// Called once during Awake after Singleton is established.
    /// 
    /// AUDIO SOURCE SETUP:
    /// - sfxSource: PlayOnAwake = false, Loop = false
    /// - musicSource: PlayOnAwake = false, Loop = true
    /// 
    /// NOTE: AudioSource components should be added in Inspector or created here
    /// </summary>
    private void InitializeAudioSources()
    {
        // Validate AudioSource components exist
        if (sfxSource == null)
        {
            Debug.LogWarning("[SoundManager] SFX AudioSource not assigned! Creating one...");
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure SFX source - PREVENT auto-play
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;

        if (musicSource == null)
        {
            Debug.LogWarning("[SoundManager] Music AudioSource not assigned! Creating one...");
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure Music source - PREVENT auto-play
        musicSource.playOnAwake = false;
        musicSource.loop = true;

        // Apply initial volumes
        UpdateVolumes();
        Debug.Log("[SoundManager] Audio sources initialized");
    }

    #endregion

    #region Settings Persistence

    /// <summary>
    /// Loads audio settings from PlayerPrefs.
    /// Called once during Awake to restore user preferences.
    /// 
    /// SAVED SETTINGS:
    /// - "SoundEnabled": 0 (disabled) or 1 (enabled) - defaults to 1
    /// - "SFXVolume": 0.0 to 1.0 - defaults to 0.7
    /// - "MusicVolume": 0.0 to 1.0 - defaults to 0.5
    /// 
    /// STORAGE LOCATION:
    /// - Windows: Registry
    /// - Mac: ~/Library/Preferences
    /// - Mobile: Platform-specific storage
    /// </summary>
    private void LoadSettings()
    {
        soundEnabled = PlayerPrefs.GetInt("SoundEnabled", 1) == 1;
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.7f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);

        UpdateVolumes();

        Debug.Log($"[SoundManager] Settings loaded: Enabled={soundEnabled}, SFX={sfxVolume:F2}, Music={musicVolume:F2}");
    }

    /// <summary>
    /// Saves current audio settings to PlayerPrefs.
    /// Called automatically when settings are changed via public methods.
    /// 
    /// PERSISTENCE:
    /// - Settings survive game restarts
    /// - PlayerPrefs.Save() ensures immediate write to disk
    /// - No need to manually call this method
    /// </summary>
    private void SaveSettings()
    {
        PlayerPrefs.SetInt("SoundEnabled", soundEnabled ? 1 : 0);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.Save(); // Force immediate write

        Debug.Log($"[SoundManager] Settings saved: Enabled={soundEnabled}, SFX={sfxVolume:F2}, Music={musicVolume:F2}");
    }

    #endregion

    #region Public Sound Methods

    /// <summary>
    /// Plays the card flip sound effect.
    /// Called by Card.cs when a card is clicked and flipped.
    /// 
    /// USAGE: SoundManager.Instance.PlayFlip();
    /// </summary>
    public void PlayFlip()
    {
        PlaySFX(flipSound);
    }

    /// <summary>
    /// Plays the match success sound effect.
    /// Called by GameManager when cards match successfully.
    /// 
    /// USAGE: SoundManager.Instance.PlayMatch();
    /// </summary>
    public void PlayMatch()
    {
        PlaySFX(matchSound);
    }

    /// <summary>
    /// Plays the mismatch sound effect.
    /// Called by GameManager when cards don't match.
    /// 
    /// USAGE: SoundManager.Instance.PlayMismatch();
    /// </summary>
    public void PlayMismatch()
    {
        PlaySFX(mismatchSound);
    }

    /// <summary>
    /// Plays the game over/victory sound effect.
    /// Called by GameManager when all cards are matched.
    /// 
    /// USAGE: SoundManager.Instance.PlayGameOver();
    /// </summary>
    public void PlayGameOver()
    {
        PlaySFX(gameOverSound);
    }

    /// <summary>
    /// Plays a sound effect using PlayOneShot.
    /// Allows multiple sounds to play simultaneously without interruption.
    /// 
    /// BEHAVIOR:
    /// - Respects soundEnabled flag (mutes if disabled)
    /// - Uses sfxVolume for volume level
    /// - PlayOneShot allows overlapping sounds (flip + match at same time)
    /// - Safe if clip is null (won't play, no error)
    /// 
    /// WHY PLAYONESHOT?
    /// - Play() stops previous sound (bad for rapid clicks)
    /// - PlayOneShot() allows multiple sounds simultaneously (good for game feel)
    /// </summary>
    /// <param name="clip">AudioClip to play</param>
    public void PlaySFX(AudioClip clip)
    {
        if (!soundEnabled || sfxSource == null || clip == null)
        {
            // Silent fail - this is intentional
            return;
        }

        sfxSource.PlayOneShot(clip);
        // Debug.Log($"[SoundManager] Playing SFX: {clip.name}");
    }

    /// <summary>
    /// Plays background music on loop.
    /// Stops current music if different clip is provided.
    /// 
    /// BEHAVIOR:
    /// - Loops continuously until stopped
    /// - Respects soundEnabled flag
    /// - Uses musicVolume for volume level
    /// - Doesn't restart if same clip is already playing
    /// 
    /// USAGE:
    /// SoundManager.Instance.PlayMusic(menuMusic);
    /// SoundManager.Instance.PlayMusic(gameplayMusic);
    /// </summary>
    /// <param name="clip">AudioClip to play as background music</param>
    public void PlayMusic(AudioClip clip)
    {
        // Safety check: ensure this is the active instance
        if (Instance != this)
        {
            Debug.LogWarning("[SoundManager] Attempted to play music on inactive instance!");
            return;
        }

        if (!soundEnabled || musicSource == null || clip == null) return;

        // Only change music if it's a different clip
        if (musicSource.clip != clip)
        {
            musicSource.Stop(); // IMPORTANT: Stop before changing clip
            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.Play();
            Debug.Log($"[SoundManager] Playing music: {clip.name}");
        }
        else if (!musicSource.isPlaying)
        {
            // Same clip but not playing - resume it
            musicSource.Play();
            Debug.Log($"[SoundManager] Resuming music: {clip.name}");
        }
    }

    /// <summary>
    /// Stops currently playing background music.
    /// Useful for menu transitions or pausing.
    /// 
    /// USAGE: SoundManager.Instance.StopMusic();
    /// </summary>
    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
            Debug.Log("[SoundManager] Music stopped");
        }
    }

    #endregion

    #region Settings Control

    /// <summary>
    /// Toggles all audio on/off.
    /// Updates volumes immediately and saves to PlayerPrefs.
    /// 
    /// USAGE:
    /// - From UI: Assign to a Toggle button's OnValueChanged
    /// - From code: SoundManager.Instance.ToggleSound();
    /// 
    /// EFFECT:
    /// - true → false: Mutes all audio (volumes set to 0)
    /// - false → true: Restores audio (volumes restored)
    /// </summary>
    public void ToggleSound()
    {
        soundEnabled = !soundEnabled;
        UpdateVolumes();
        SaveSettings();

        Debug.Log($"[SoundManager] Sound {(soundEnabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Sets the sound effects volume level.
    /// Automatically clamps to 0-1 range and saves setting.
    /// 
    /// USAGE:
    /// - From UI: Assign to Slider's OnValueChanged
    /// - From code: SoundManager.Instance.SetSFXVolume(0.8f);
    /// 
    /// EXAMPLE SLIDER SETUP:
    /// - Min Value: 0
    /// - Max Value: 1
    /// - OnValueChanged: SoundManager.Instance.SetSFXVolume
    /// </summary>
    /// <param name="volume">Volume level (0.0 = silent, 1.0 = full volume)</param>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
        SaveSettings();

        Debug.Log($"[SoundManager] SFX volume set to {sfxVolume:F2}");
    }

    /// <summary>
    /// Sets the background music volume level.
    /// Automatically clamps to 0-1 range and saves setting.
    /// 
    /// USAGE:
    /// - From UI: Assign to Slider's OnValueChanged
    /// - From code: SoundManager.Instance.SetMusicVolume(0.5f);
    /// 
    /// EXAMPLE SLIDER SETUP:
    /// - Min Value: 0
    /// - Max Value: 1
    /// - OnValueChanged: SoundManager.Instance.SetMusicVolume
    /// </summary>
    /// <param name="volume">Volume level (0.0 = silent, 1.0 = full volume)</param>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
        SaveSettings();

        Debug.Log($"[SoundManager] Music volume set to {musicVolume:F2}");
    }

    /// <summary>
    /// Applies current volume settings to AudioSource components.
    /// Respects soundEnabled flag (mutes if disabled).
    /// 
    /// VOLUME CALCULATION:
    /// - If soundEnabled: Use specified volume (sfxVolume/musicVolume)
    /// - If !soundEnabled: Force volume to 0
    /// 
    /// Called automatically when:
    /// - Settings are loaded
    /// - Sound is toggled
    /// - Volume sliders are changed
    /// </summary>
    private void UpdateVolumes()
    {
        if (sfxSource != null)
            sfxSource.volume = soundEnabled ? sfxVolume : 0f;

        if (musicSource != null)
            musicSource.volume = soundEnabled ? musicVolume : 0f;

        // Debug.Log($"[SoundManager] Volumes updated: SFX={sfxSource?.volume:F2}, Music={musicSource?.volume:F2}");
    }

    #endregion

    #region Public Getters (Optional - for UI sync)

    /// <summary>
    /// Gets the current SFX volume level.
    /// Useful for initializing UI sliders to match saved settings.
    /// </summary>
    public float GetSFXVolume() => sfxVolume;

    /// <summary>
    /// Gets the current music volume level.
    /// Useful for initializing UI sliders to match saved settings.
    /// </summary>
    public float GetMusicVolume() => musicVolume;

    /// <summary>
    /// Gets the current sound enabled state.
    /// Useful for initializing UI toggles to match saved settings.
    /// </summary>
    public bool IsSoundEnabled() => soundEnabled;

    #endregion

    #region Editor Utilities

#if UNITY_EDITOR
    /// <summary>
    /// Debug method: Test flip sound in editor.
    /// Right-click SoundManager in Inspector > Test Flip Sound
    /// </summary>
    [ContextMenu("Test Flip Sound")]
    private void TestFlipSound()
    {
        PlayFlip();
    }

    /// <summary>
    /// Debug method: Test match sound in editor.
    /// Right-click SoundManager in Inspector > Test Match Sound
    /// </summary>
    [ContextMenu("Test Match Sound")]
    private void TestMatchSound()
    {
        PlayMatch();
    }

    /// <summary>
    /// Debug method: Test mismatch sound in editor.
    /// Right-click SoundManager in Inspector > Test Mismatch Sound
    /// </summary>
    [ContextMenu("Test Mismatch Sound")]
    private void TestMismatchSound()
    {
        PlayMismatch();
    }

    /// <summary>
    /// Debug method: Test game over sound in editor.
    /// Right-click SoundManager in Inspector > Test GameOver Sound
    /// </summary>
    [ContextMenu("Test GameOver Sound")]
    private void TestGameOverSound()
    {
        PlayGameOver();
    }

    /// <summary>
    /// Debug method: Clear saved audio settings.
    /// Right-click SoundManager in Inspector > Clear Saved Settings
    /// </summary>
    [ContextMenu("Clear Saved Settings")]
    private void ClearSavedSettings()
    {
        PlayerPrefs.DeleteKey("SoundEnabled");
        PlayerPrefs.DeleteKey("SFXVolume");
        PlayerPrefs.DeleteKey("MusicVolume");
        PlayerPrefs.Save();
        Debug.Log("[SoundManager] Saved settings cleared");
    }

    /// <summary>
    /// Debug method: Log current audio state.
    /// Right-click SoundManager in Inspector > Log Audio State
    /// </summary>
    [ContextMenu("Log Audio State")]
    private void LogAudioState()
    {
        Debug.Log($"[SoundManager] Audio State:");
        Debug.Log($"  Sound Enabled: {soundEnabled}");
        Debug.Log($"  SFX Volume: {sfxVolume:F2}");
        Debug.Log($"  Music Volume: {musicVolume:F2}");
        Debug.Log($"  SFX Source: {(sfxSource != null ? "OK" : "MISSING")}");
        Debug.Log($"  Music Source: {(musicSource != null ? "OK" : "MISSING")}");
        Debug.Log($"  Clips Assigned: Flip={flipSound != null}, Match={matchSound != null}, " +
                  $"Mismatch={mismatchSound != null}, GameOver={gameOverSound != null}");
    }
#endif

    #endregion
}

/*
 * USAGE EXAMPLES:
 * 
 * 1. Play sound effects:
 *    SoundManager.Instance.PlayFlip();
 *    SoundManager.Instance.PlayMatch();
 * 
 * 2. Control volumes:
 *    SoundManager.Instance.SetSFXVolume(0.8f);
 *    SoundManager.Instance.SetMusicVolume(0.5f);
 * 
 * 3. Toggle sound:
 *    SoundManager.Instance.ToggleSound();
 * 
 * 4. Play background music:
 *    SoundManager.Instance.PlayMusic(menuMusicClip);
 *    SoundManager.Instance.StopMusic();
 * 
 * 5. Get current settings (for UI initialization):
 *    float currentSFX = SoundManager.Instance.GetSFXVolume();
 *    bool soundOn = SoundManager.Instance.IsSoundEnabled();
 * 
 * SETUP INSTRUCTIONS:
 * 
 * 1. Create GameObject in first scene:
 *    - GameObject > Create Empty
 *    - Name it "SoundManager"
 *    - Add SoundManager.cs script
 * 
 * 2. AudioSources are auto-created if missing, OR manually add:
 *    - Add Component > Audio > Audio Source (for SFX)
 *    - Add Component > Audio > Audio Source (for Music)
 *    - Assign to sfxSource and musicSource fields
 * 
 * 3. Assign AudioClips in Inspector:
 *    - Drag flip sound to flipSound
 *    - Drag match sound to matchSound
 *    - Drag mismatch sound to mismatchSound
 *    - Drag game over sound to gameOverSound
 * 
 * 4. Configure AudioSource settings:
 *    SFX Source:
 *    - Play On Awake: OFF
 *    - Loop: OFF
 *    - Volume: Will be controlled by script
 *    
 *    Music Source:
 *    - Play On Awake: OFF
 *    - Loop: ON (for background music)
 *    - Volume: Will be controlled by script
 * 
 * UI INTEGRATION:
 * 
 * Settings Panel with Sliders:
 * 
 * 1. SFX Volume Slider:
 *    - Min Value: 0
 *    - Max Value: 1
 *    - Value: 0.7 (default)
 *    - OnValueChanged: SoundManager.Instance.SetSFXVolume
 * 
 * 2. Music Volume Slider:
 *    - Min Value: 0
 *    - Max Value: 1
 *    - Value: 0.5 (default)
 *    - OnValueChanged: SoundManager.Instance.SetMusicVolume
 * 
 * 3. Sound Toggle:
 *    - IsOn: true (default)
 *    - OnValueChanged: SoundManager.Instance.ToggleSound
 * 
 * Initialize UI from saved settings:
 * 
 * void Start() {
 *     sfxSlider.value = SoundManager.Instance.GetSFXVolume();
 *     musicSlider.value = SoundManager.Instance.GetMusicVolume();
 *     soundToggle.isOn = SoundManager.Instance.IsSoundEnabled();
 * }
 * 
 * AUDIO CLIP GUIDELINES:
 * 
 * Recommended formats:
 * - SFX: WAV or OGG (uncompressed for low latency)
 * - Music: MP3 or OGG (compressed to save space)
 * 
 * Recommended settings in Inspector:
 * - Flip Sound: Load Type = Decompress On Load, Compression Format = PCM
 * - Match Sound: Load Type = Decompress On Load, Compression Format = PCM
 * - Mismatch Sound: Load Type = Decompress On Load, Compression Format = PCM
 * - GameOver Sound: Load Type = Decompress On Load, Compression Format = Vorbis
 * - Music: Load Type = Streaming, Compression Format = Vorbis
 * 
 * COMMON ISSUES & SOLUTIONS:
 * 
 * Issue: No sound playing
 * - Check if soundEnabled = true
 * - Check if AudioClips are assigned
 * - Check if volumes are > 0
 * - Check AudioListener exists in scene
 * 
 * Issue: Sounds cut off each other
 * - Ensure using PlayOneShot (not Play)
 * - Check sfxSource is not looping
 * 
 * Issue: Settings not persisting
 * - Check PlayerPrefs.Save() is called
 * - Verify platform supports PlayerPrefs
 * 
 * Issue: SoundManager destroyed on scene load
 * - Ensure DontDestroyOnLoad is called in Awake
 * - Check only one instance exists
 * 
 * PERFORMANCE NOTES:
 * 
 * - PlayOneShot is efficient for short SFX
 * - Streaming recommended for long music tracks
 * - Multiple SFX can play simultaneously (CPU impact minimal)
 * - PlayerPrefs operations are fast (no noticeable lag)
 * 
 * RECOMMENDED IMPROVEMENTS:
 * 
 * 1. Audio Mixer integration:
 *    - Create Audio Mixer asset
 *    - Add SFX and Music groups
 *    - Control via SetFloat instead of direct volume
 * 
 * 2. Fade transitions:
 *    - Fade out old music, fade in new music
 *    - Smoother menu → game transitions
 * 
 * 3. Sound pools:
 *    - For rapidly repeating sounds
 *    - Prevents audio crackling
 * 
 * 4. Dynamic mixing:
 *    - Duck music when SFX plays
 *    - Lower music during important moments
 */