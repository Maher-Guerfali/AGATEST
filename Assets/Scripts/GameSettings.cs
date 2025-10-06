using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Card Game/Game Settings")]
public class GameSettings : ScriptableObject
{
    [Header("Grid Configuration")]
    [Min(2)] public int rows = 4;
    [Min(2)] public int cols = 4;
    [Min(2)] public int pairSize = 2;

    [Header("Timing")]
    [Range(0.1f, 3f)] public float revealDelay = 0.8f;
    [Range(0.5f, 10f)] public float previewDuration = 3f;

    [Header("Scoring")]
    [Min(1)] public int baseScore = 100;
    public bool enableCombo = true;

    [Header("Game Rules")]
    public bool enablePreview = true;
    public bool allowContinuousFlipping = true;

    /// <summary>
    /// Validates the grid settings and returns true if they are valid
    /// </summary>
    public bool IsValidGrid()
    {
        int totalCards = rows * cols;
        return totalCards % pairSize == 0 && totalCards >= 4;
    }

    /// <summary>
    /// Gets the total number of cards for the current grid
    /// </summary>
    public int GetTotalCards()
    {
        return rows * cols;
    }

    /// <summary>
    /// Gets the number of pairs/groups for the current grid
    /// </summary>
    public int GetTotalPairs()
    {
        return GetTotalCards() / pairSize;
    }

    /// <summary>
    /// Clamps the grid values to safe ranges
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
    /// Resets to default values
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
}