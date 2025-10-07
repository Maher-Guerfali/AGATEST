using UnityEngine;

/// <summary>
/// ScriptableObject that stores all configurable game settings for the card matching game.
/// Create via: Assets > Create > Card Game > Game Settings
/// 
/// This centralized configuration allows designers to modify game parameters without touching code.
/// Multiple settings profiles can be created for different difficulty levels.
/// 
/// Author: [Your Team Name]
/// Last Modified: 2025
/// </summary>
[CreateAssetMenu(fileName = "GameSettings", menuName = "Card Game/Game Settings")]
public class GameSettings : ScriptableObject
{
    #region Grid Configuration

    [Header("Grid Configuration")]
    [Tooltip("Number of rows in the card grid. Must be at least 2.")]
    [Min(2)] public int rows = 4;

    [Tooltip("Number of columns in the card grid. Must be at least 2.")]
    [Min(2)] public int cols = 4;

    [Tooltip("Number of cards that must match (2 = pairs, 3 = triplets, etc.). Total cards must be divisible by this number.")]
    [Min(2)] public int pairSize = 2;

    #endregion

    #region Timing Settings

    [Header("Timing")]
    [Tooltip("Time in seconds before mismatched cards flip back. Range: 0.1 to 3 seconds.")]
    [Range(0.1f, 3f)] public float revealDelay = 0.8f;

    [Tooltip("Duration in seconds that all cards are shown at game start. Range: 0.5 to 10 seconds.")]
    [Range(0.5f, 10f)] public float previewDuration = 3f;

    #endregion

    #region Scoring Configuration

    [Header("Scoring")]
    [Tooltip("Base points awarded per successful match. Multiplied by combo if enabled.")]
    [Min(1)] public int baseScore = 100;

    [Tooltip("If true, consecutive matches increase score multiplier. Resets on mismatch.")]
    public bool enableCombo = true;

    #endregion

    #region Game Rules

    [Header("Game Rules")]
    [Tooltip("If true, all cards are revealed for previewDuration at game start.")]
    public bool enablePreview = true;

    [Tooltip("If true, players can flip multiple cards before comparison. If false, cards auto-hide after comparison.")]
    public bool allowContinuousFlipping = true;

    #endregion

    #region Validation Methods

    /// <summary>
    /// Validates the grid settings and returns true if they form a playable game.
    /// 
    /// Validation Rules:
    /// - Total cards must be divisible by pairSize (ensures all cards can be matched)
    /// - Minimum of 4 cards required for gameplay
    /// 
    /// Example: 4x4 grid (16 cards) with pairSize=2 is valid (8 pairs)
    /// Example: 5x3 grid (15 cards) with pairSize=2 is invalid (7.5 pairs - impossible!)
    /// </summary>
    /// <returns>True if grid configuration is valid and playable</returns>
    public bool IsValidGrid()
    {
        int totalCards = rows * cols;
        return totalCards % pairSize == 0 && totalCards >= 4;
    }

    /// <summary>
    /// Gets the total number of cards that will be created for the current grid configuration.
    /// </summary>
    /// <returns>Total number of cards (rows * cols)</returns>
    public int GetTotalCards()
    {
        return rows * cols;
    }

    /// <summary>
    /// Gets the number of matching groups for the current grid.
    /// </summary>
    /// <returns>Number of groups (total cards / pairSize)</returns>
    /// <example>
    /// 16 cards with pairSize=2 returns 8 (8 pairs)
    /// 18 cards with pairSize=3 returns 6 (6 triplets)
    /// </example>
    public int GetTotalPairs()
    {
        return GetTotalCards() / pairSize;
    }

    /// <summary>
    /// Clamps all values to safe, playable ranges to prevent configuration errors.
    /// Call this before starting a game to ensure no invalid values are present.
    /// 
    /// Clamped Ranges:
    /// - Rows: 2 to 10
    /// - Cols: 2 to 10
    /// - PairSize: 2 to 4
    /// - RevealDelay: 0.1 to 3 seconds
    /// - PreviewDuration: 0.5 to 10 seconds
    /// - BaseScore: Minimum 1
    /// </summary>
    public void ValidateAndClampValues()
    {
        rows = Mathf.Clamp(rows, 2, 10);
        cols = Mathf.Clamp(cols, 2, 10);
        pairSize = Mathf.Clamp(pairSize, 2, 4);
        revealDelay = Mathf.Clamp(revealDelay, 0.1f, 3f);
        previewDuration = Mathf.Clamp(previewDuration, 0.5f, 10f);
        baseScore = Mathf.Max(baseScore, 1);
    }

    /// <summary>
    /// Resets all settings to default values for a standard 4x4 pair matching game.
    /// Useful for creating a "Reset to Defaults" button in settings UI.
    /// </summary>
    public void ResetToDefaults()
    {
        rows = 4;
        cols = 4;
        pairSize = 2;
        revealDelay = 0.8f;
        previewDuration = 3f;
        baseScore = 100;
        enableCombo = true;
        enablePreview = true;
        allowContinuousFlipping = true;
    }

    #endregion

    #region Unity Editor Utilities

#if UNITY_EDITOR
    /// <summary>
    /// Called when the ScriptableObject is loaded or a value is changed in the Inspector.
    /// Automatically validates settings to prevent invalid configurations.
    /// </summary>
    private void OnValidate()
    {
        ValidateAndClampValues();

        // Warn about invalid grid configurations
        if (!IsValidGrid())
        {
            Debug.LogWarning($"[GameSettings] Invalid grid configuration! " +
                           $"{rows}x{cols} = {GetTotalCards()} cards cannot be evenly divided into groups of {pairSize}. " +
                           $"Adjust grid size or pairSize.");
        }
    }
#endif

    #endregion
}

/*
 * USAGE EXAMPLES:
 * 
 * 1. Create a new GameSettings asset:
 *    Right-click in Project > Create > Card Game > Game Settings
 * 
 * 2. Access settings in code:
 *    public GameSettings settings;
 *    int totalCards = settings.GetTotalCards();
 * 
 * 3. Create difficulty presets:
 *    - Easy.asset: 3x4 grid, 3s preview, 2s reveal delay
 *    - Normal.asset: 4x4 grid, 3s preview, 0.8s reveal delay
 *    - Hard.asset: 6x5 grid, 2s preview, 0.5s reveal delay
 * 
 * 4. Validate before use:
 *    if (settings.IsValidGrid()) {
 *        StartGame(settings);
 *    }
 */