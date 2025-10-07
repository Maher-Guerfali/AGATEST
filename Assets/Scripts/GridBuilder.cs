using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// Manages card grid creation, layout, sizing, and state persistence.
/// Handles dynamic card sizing based on grid dimensions to ensure optimal display.
/// 
/// RESPONSIBILITIES:
/// - Build card grid from prefab
/// - Calculate optimal card sizes for different grid configurations
/// - Configure GridLayoutGroup for proper spacing
/// - Shuffle card IDs for randomization
/// - Save/restore grid state
/// - Track match completion
/// 
/// SIZING ALGORITHM:
/// - Small grids (≤3 rows, ≤5 cols): Use default size (190x240) or fit to container
/// - Large grids (≥6 rows/cols): Use square cards max 130x130
/// - Medium grids: Interpolate between default and small
/// - Always respects minimum size (100x100)
/// - Adds 10% padding between cards
/// 
/// Author: [Your Team Name]
/// Last Modified: 2025
/// </summary>
public class GridBuilder : MonoBehaviour
{
    #region Inspector References

    [Header("Data")]
    [Tooltip("ScriptableObject containing all card sprites (front and back)")]
    public CardSet cardSet;

    [Tooltip("ScriptableObject containing grid configuration (rows, cols, pairSize)")]
    public GameSettings settings;

    [Header("Card Sizing")]
    [SerializeField]
    [Tooltip("Default card size for small grids (width x height in pixels)")]
    private Vector2 defaultCardSize = new Vector2(190f, 240f);

    [SerializeField]
    [Tooltip("Minimum allowed card size to maintain readability")]
    private Vector2 minCardSize = new Vector2(100f, 100f);

    [SerializeField]
    [Tooltip("Percentage of space to use for padding between cards (0.1 = 10%)")]
    private float paddingPercentage = 0.1f;

    #endregion

    #region Private Variables

    /// <summary>
    /// List of all active cards in the current grid.
    /// Used for state capture, match checking, and cleanup.
    /// </summary>
    private List<Card> cards = new List<Card>();

    #endregion

    #region Grid Building

    /// <summary>
    /// Builds a new grid of cards inside the given container using CardSet sprites.
    /// 
    /// ALGORITHM:
    /// 1. Clear any existing grid
    /// 2. Validate grid size is divisible by pairSize
    /// 3. Calculate optimal card size for container
    /// 4. Configure GridLayoutGroup with calculated size and spacing
    /// 5. Generate shuffled pool of card IDs
    /// 6. Instantiate cards and assign sprites
    /// 7. Set card sizes
    /// 
    /// ID GENERATION:
    /// For 4x4 grid (16 cards) with pairSize=2:
    /// - groupCount = 16 / 2 = 8 pairs
    /// - poolIds = [0,0,1,1,2,2,3,3,4,4,5,5,6,6,7,7]
    /// - Shuffle the array randomly
    /// 
    /// For 6x3 grid (18 cards) with pairSize=3:
    /// - groupCount = 18 / 3 = 6 triplets
    /// - poolIds = [0,0,0,1,1,1,2,2,2,3,3,3,4,4,4,5,5,5]
    /// - Shuffle the array randomly
    /// </summary>
    /// <param name="cardPrefab">Prefab to instantiate for each card</param>
    /// <param name="container">RectTransform container (must have GridLayoutGroup)</param>
    public void BuildGrid(Card cardPrefab, RectTransform container)
    {
        ClearGrid();
        int total = settings.rows * settings.cols;

        // Validate grid configuration
        if (total % settings.pairSize != 0)
        {
            Debug.LogWarning($"[GridBuilder] Grid size {total} not divisible by pairSize {settings.pairSize}. " +
                           "Some cards may not match!");
        }

        // Calculate optimal card size for current grid
        Vector2 cardSize = CalculateCardSize(container);
        Debug.Log($"[GridBuilder] Calculated card size for {settings.rows}x{settings.cols} grid: {cardSize}");

        // Setup GridLayoutGroup with calculated size
        SetupGridLayout(container, cardSize);

        // --- Prepare deck with shuffled IDs
        List<int> poolIds = new List<int>();
        int groupCount = total / settings.pairSize;

        // Generate ID pool (each ID appears pairSize times)
        for (int i = 0; i < groupCount; i++)
        {
            for (int j = 0; j < settings.pairSize; j++)
                poolIds.Add(i); // Assign same ID for pair/triplet/etc.
        }

        // Shuffle for randomization
        Shuffle(poolIds);

        // --- Instantiate cards
        for (int i = 0; i < total; i++)
        {
            // Create card instance
            var c = Instantiate(cardPrefab, container);

            // Get ID and corresponding sprite
            int id = poolIds[i];
            Sprite frontSprite = cardSet.frontSprites[id % cardSet.frontSprites.Count];

            // Initialize card
            c.Initialize(id, frontSprite, cardSet.backSprite);

            // Set card size
            RectTransform cardRect = c.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.sizeDelta = cardSize;
            }

            cards.Add(c);
        }

        Debug.Log($"[GridBuilder] Built {cards.Count} cards in {settings.rows}x{settings.cols} grid");
    }

    #endregion

    #region Card Sizing System

    /// <summary>
    /// Calculates optimal card size based on container size and grid dimensions.
    /// Implements intelligent sizing to ensure cards fit well on all screen sizes.
    /// 
    /// SIZING RULES:
    /// 
    /// 1. SMALL GRIDS (≤3 rows AND ≤5 cols):
    ///    - Use defaultCardSize (190x240) if it fits
    ///    - Scale down proportionally if container too small
    ///    Example: 2x2, 3x4 grids
    /// 
    /// 2. LARGE GRIDS (≥6 rows OR ≥6 cols):
    ///    - Use square cards for uniformity
    ///    - Max size: 130x130 pixels
    ///    - Scales down if container too small
    ///    Example: 6x5, 8x8 grids
    /// 
    /// 3. MEDIUM GRIDS (4-5 rows/cols):
    ///    - Interpolate between default and small
    ///    - Scales based on grid dimensions
    ///    Example: 4x4, 5x4 grids
    /// 
    /// ALWAYS:
    /// - Respects minCardSize (100x100) minimum
    /// - Accounts for paddingPercentage (10% default)
    /// - Centers in container
    /// </summary>
    /// <param name="container">Container RectTransform to fit cards into</param>
    /// <returns>Optimal card size as Vector2 (width, height)</returns>
    private Vector2 CalculateCardSize(RectTransform container)
    {
        // Get container dimensions
        Vector2 containerSize = container.rect.size;

        // If container size is not available yet, use a default reference size
        if (containerSize.x <= 0 || containerSize.y <= 0)
        {
            containerSize = new Vector2(1080f, 1920f); // Common mobile portrait reference
            Debug.LogWarning("[GridBuilder] Container size not available, using default reference size (1080x1920)");
        }

        // Calculate available space per card (accounting for padding)
        float availableWidth = containerSize.x * (1f - paddingPercentage);
        float availableHeight = containerSize.y * (1f - paddingPercentage);

        float cardWidth = availableWidth / settings.cols;
        float cardHeight = availableHeight / settings.rows;

        // Apply specific sizing rules
        Vector2 calculatedSize;

        // RULE 1: Small grids - use default size or smaller
        if (settings.rows <= 3 && settings.cols <= 5)
        {
            calculatedSize = new Vector2(
                Mathf.Min(defaultCardSize.x, cardWidth),
                Mathf.Min(defaultCardSize.y, cardHeight)
            );
            Debug.Log($"[GridBuilder] Small grid sizing applied");
        }
        // RULE 2: Large grids - use square cards with max 130px
        else if (settings.rows >= 6 || settings.cols >= 6)
        {
            float squareSize = Mathf.Min(cardWidth, cardHeight);
            squareSize = Mathf.Min(squareSize, 130f); // Max size for large grids
            calculatedSize = new Vector2(squareSize, squareSize);
            Debug.Log($"[GridBuilder] Large grid sizing applied - square cards");
        }
        // RULE 3: Medium grids - interpolate between default and small
        else
        {
            float factor = Mathf.Max(settings.rows, settings.cols) / 6f; // Scale factor
            float width = Mathf.Lerp(defaultCardSize.x, 130f, factor);
            float height = Mathf.Lerp(defaultCardSize.y, 130f, factor);

            calculatedSize = new Vector2(
                Mathf.Min(width, cardWidth),
                Mathf.Min(height, cardHeight)
            );
            Debug.Log($"[GridBuilder] Medium grid sizing applied with factor {factor:F2}");
        }

        // Ensure minimum size for readability
        calculatedSize.x = Mathf.Max(calculatedSize.x, minCardSize.x);
        calculatedSize.y = Mathf.Max(calculatedSize.y, minCardSize.y);

        return calculatedSize;
    }

    /// <summary>
    /// Sets up the GridLayoutGroup component with the calculated card size and spacing.
    /// Configures proper padding and alignment for centered, evenly-spaced cards.
    /// 
    /// CONFIGURATION:
    /// - Constraint: Fixed column count
    /// - Cell Size: Calculated card size
    /// - Spacing: Dynamic based on available space
    /// - Padding: Half of spacing for centering
    /// - Alignment: Middle Center
    /// 
    /// SPACING CALCULATION:
    /// spacingX = (containerWidth - (cardWidth * cols)) / (cols + 1)
    /// This ensures equal gaps between cards and at edges.
    /// </summary>
    /// <param name="container">Container with GridLayoutGroup</param>
    /// <param name="cardSize">Size to use for each card cell</param>
    private void SetupGridLayout(RectTransform container, Vector2 cardSize)
    {
        var layout = container.GetComponent<GridLayoutGroup>();
        if (layout == null)
        {
            Debug.LogError("[GridBuilder] Container does not have GridLayoutGroup component!");
            return;
        }

        // Configure grid constraint
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = settings.cols;
        layout.cellSize = cardSize;

        // Calculate spacing based on card size and container
        Vector2 containerSize = container.rect.size;
        if (containerSize.x > 0 && containerSize.y > 0)
        {
            // Calculate gaps to fill remaining space evenly
            float spacingX = (containerSize.x - (cardSize.x * settings.cols)) / (settings.cols + 1);
            float spacingY = (containerSize.y - (cardSize.y * settings.rows)) / (settings.rows + 1);

            // Minimum 5px spacing for visual clarity
            spacingX = Mathf.Max(5f, spacingX);
            spacingY = Mathf.Max(5f, spacingY);

            layout.spacing = new Vector2(spacingX, spacingY);

            // Padding is half of spacing to center the grid
            layout.padding = new RectOffset(
                Mathf.RoundToInt(spacingX * 0.5f),
                Mathf.RoundToInt(spacingX * 0.5f),
                Mathf.RoundToInt(spacingY * 0.5f),
                Mathf.RoundToInt(spacingY * 0.5f)
            );

            Debug.Log($"[GridBuilder] Layout configured: spacing=({spacingX:F1}, {spacingY:F1}), " +
                     $"padding=({layout.padding.left}, {layout.padding.top})");
        }
        else
        {
            // Default spacing if container size not available
            float defaultSpacing = cardSize.x * 0.05f; // 5% of card width
            layout.spacing = new Vector2(defaultSpacing, defaultSpacing);
            layout.padding = new RectOffset(10, 10, 10, 10);

            Debug.LogWarning("[GridBuilder] Using default spacing (container size unavailable)");
        }

        // Center the grid
        layout.childAlignment = TextAnchor.MiddleCenter;
    }

    /// <summary>
    /// Public method to update grid size and rebuild with new card sizes.
    /// Useful for dynamic difficulty adjustment or settings changes.
    /// </summary>
    /// <param name="newRows">New number of rows</param>
    /// <param name="newCols">New number of columns</param>
    public void UpdateGridSize(int newRows, int newCols)
    {
        settings.rows = newRows;
        settings.cols = newCols;

        Debug.Log($"[GridBuilder] Grid size updated to {newRows}x{newCols}");

        if (GameManager.Instance != null)
        {
            BuildGrid(GameManager.Instance.cardPrefab, GameManager.Instance.gridContainer);
        }
        else
        {
            Debug.LogWarning("[GridBuilder] Cannot rebuild grid - GameManager.Instance is null");
        }
    }

    #endregion

    #region State Persistence

    /// <summary>
    /// Captures the current state of the grid and all cards for saving.
    /// 
    /// CAPTURED DATA:
    /// - Grid dimensions (rows, cols)
    /// - Game score and combo
    /// - Each card's state (faceId, isMatched, isRevealed)
    /// 
    /// This data can be serialized to JSON and saved to disk.
    /// </summary>
    /// <param name="score">Current game score</param>
    /// <param name="combo">Current combo multiplier</param>
    /// <returns>GameState object containing all grid and card data</returns>
    public GameState CaptureState(int score, int combo)
    {
        GameState s = new GameState
        {
            rows = settings.rows,
            cols = settings.cols,
            score = score,
            combo = combo,
            cards = cards.Select(c => new CardState
            {
                faceId = c.faceId,
                isMatched = c.IsMatched,
                isRevealed = c.IsRevealed
            }).ToList()
        };

        Debug.Log($"[GridBuilder] State captured: {s.cards.Count} cards, " +
                 $"{s.cards.Count(c => c.isMatched)} matched, " +
                 $"{s.cards.Count(c => c.isRevealed && !c.isMatched)} revealed");

        return s;
    }

    /// <summary>
    /// Restores card states from a saved GameState.
    /// IMPORTANT: Must be called AFTER BuildGrid() has created the cards.
    /// 
    /// PROCESS:
    /// 1. Validate state and card count match
    /// 2. For each card:
    ///    a. Re-initialize with saved faceId and sprites
    ///    b. If matched: Reveal then mark as matched
    ///    c. If revealed: Show front face
    ///    d. If hidden: Show back face
    /// 
    /// ORDER MATTERS:
    /// - For matched cards: Must call Reveal() BEFORE MarkMatched()
    /// - This ensures matched cards stay visible and uninteractable
    /// 
    /// USAGE:
    /// GameState state = SaveSystem.Load();
    /// gridBuilder.BuildGrid(prefab, container);  // Create cards first
    /// gridBuilder.RestoreState(state);           // Then restore their states
    /// </summary>
    /// <param name="state">GameState to restore from</param>
    public void RestoreState(GameState state)
    {
        if (state == null || state.cards == null)
        {
            Debug.LogError("[GridBuilder] Cannot restore state: state or cards is null");
            return;
        }

        Debug.Log($"[GridBuilder] Restoring state for {cards.Count} cards, saved states: {state.cards.Count}");

        // Validate card count matches
        if (state.cards.Count != cards.Count)
        {
            Debug.LogError($"[GridBuilder] Card count mismatch! Expected {state.cards.Count}, but have {cards.Count} cards. " +
                          "Grid may have been rebuilt with different dimensions.");
            return;
        }

        // Apply saved states to the existing cards
        for (int i = 0; i < cards.Count && i < state.cards.Count; i++)
        {
            var cardState = state.cards[i];
            var card = cards[i];

            Debug.Log($"[GridBuilder] Restoring card {i}: faceId={cardState.faceId}, " +
                     $"revealed={cardState.isRevealed}, matched={cardState.isMatched}");

            // Re-initialize the card with correct face ID and sprites
            Sprite frontSprite = cardSet.frontSprites[cardState.faceId % cardSet.frontSprites.Count];
            card.Initialize(cardState.faceId, frontSprite, cardSet.backSprite);

            // Apply the saved state - ORDER MATTERS!
            if (cardState.isMatched)
            {
                // For matched cards: first reveal them, then mark as matched
                card.Reveal(); // Show the front face
                card.MarkMatched(); // Mark as matched (keeps them visible and uninteractable)
                Debug.Log($"[GridBuilder] Card {i} restored as MATCHED and VISIBLE");
            }
            else if (cardState.isRevealed)
            {
                card.Reveal(); // Show the front face for currently revealed cards
                Debug.Log($"[GridBuilder] Card {i} restored as REVEALED");
            }
            else
            {
                card.HideInstant(); // Make sure it's face down
                Debug.Log($"[GridBuilder] Card {i} restored as HIDDEN");
            }
        }

        Debug.Log("[GridBuilder] Card states restored successfully");
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Checks if all cards in the grid have been matched.
    /// Used by GameManager to detect win condition.
    /// </summary>
    /// <returns>True if all cards are matched, false otherwise</returns>
    public bool AllMatched()
    {
        bool allMatched = cards.All(c => c.IsMatched);

        if (allMatched)
        {
            Debug.Log("[GridBuilder] All cards matched!");
        }

        return allMatched;
    }

    /// <summary>
    /// Clears the current grid by destroying all card GameObjects.
    /// Called before building a new grid.
    /// </summary>
    public void ClearGrid()
    {
        Debug.Log($"[GridBuilder] Clearing grid ({cards.Count} cards)");

        foreach (var c in cards)
        {
            if (c != null) Destroy(c.gameObject);
        }
        cards.Clear();
    }

    /// <summary>
    /// Shuffles a list using Fisher-Yates algorithm.
    /// Ensures uniform random distribution.
    /// 
    /// ALGORITHM:
    /// For each position i from 0 to n-1:
    ///   - Pick random index r between i and n-1
    ///   - Swap elements at i and r
    /// 
    /// Time Complexity: O(n)
    /// </summary>
    /// <typeparam name="T">Type of list elements</typeparam>
    /// <param name="list">List to shuffle in-place</param>
    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]); // Tuple swap
        }

        Debug.Log($"[GridBuilder] Shuffled {list.Count} items");
    }

    #endregion

    #region Editor Utilities

#if UNITY_EDITOR
    /// <summary>
    /// Debug method: Test small grid configuration.
    /// Right-click GridBuilder in Inspector > Test Small Grid (2x2)
    /// </summary>
    [ContextMenu("Test Small Grid (2x2)")]
    public void TestSmallGrid()
    {
        Debug.Log("[GridBuilder] Testing small grid (2x2)");
        UpdateGridSize(2, 2);
    }

    /// <summary>
    /// Debug method: Test large grid configuration.
    /// Right-click GridBuilder in Inspector > Test Large Grid (6x5)
    /// </summary>
    [ContextMenu("Test Large Grid (6x5)")]
    public void TestLargeGrid()
    {
        Debug.Log("[GridBuilder] Testing large grid (6x5)");
        UpdateGridSize(6, 5);
    }

    /// <summary>
    /// Debug method: Test medium grid configuration.
    /// Right-click GridBuilder in Inspector > Test Medium Grid (4x4)
    /// </summary>
    [ContextMenu("Test Medium Grid (4x4)")]
    public void TestMediumGrid()
    {
        Debug.Log("[GridBuilder] Testing medium grid (4x4)");
        UpdateGridSize(4, 4);
    }

    /// <summary>
    /// Debug method: Log current grid state.
    /// Right-click GridBuilder in Inspector > Log Grid State
    /// </summary>
    [ContextMenu("Log Grid State")]
    public void LogGridState()
    {
        Debug.Log($"[GridBuilder] Current State:");
        Debug.Log($"  Grid Size: {settings.rows}x{settings.cols} = {cards.Count} cards");
        Debug.Log($"  Matched: {cards.Count(c => c.IsMatched)} cards");
        Debug.Log($"  Revealed: {cards.Count(c => c.IsRevealed && !c.IsMatched)} cards");
        Debug.Log($"  Hidden: {cards.Count(c => !c.IsRevealed && !c.IsMatched)} cards");
    }
#endif

    #endregion
}

/*
 * USAGE EXAMPLES:
 * 
 * 1. Build a new grid:
 *    gridBuilder.BuildGrid(cardPrefab, gridContainer);
 * 
 * 2. Change grid size dynamically:
 *    gridBuilder.UpdateGridSize(6, 5); // 6 rows, 5 columns
 * 
 * 3. Save grid state:
 *    GameState state = gridBuilder.CaptureState(score, combo);
 *    SaveSystem.Save(state);
 * 
 * 4. Load grid state:
 *    GameState state = SaveSystem.Load();
 *    gridBuilder.BuildGrid(cardPrefab, gridContainer); // Create cards
 *    gridBuilder.RestoreState(state);                  // Restore states
 * 
 * 5. Check win condition:
 *    if (gridBuilder.AllMatched()) {
 *        // Game complete!
 *    }
 * 
 * CARD SIZING EXAMPLES:
 * 
 * 2x2 grid (4 cards):
 * - Uses defaultCardSize (190x240) or scales down to fit
 * 
 * 4x4 grid (16 cards):
 * - Uses interpolated size between default and 130px
 * 
 * 6x5 grid (30 cards):
 * - Uses square cards, max 130x130
 * 
 * 8x8 grid (64 cards):
 * - Uses small square cards to fit all in view
 * 
 * INTEGRATION NOTES:
 * 
 * - Requires GameSettings ScriptableObject with rows, cols, pairSize
 * - Requires CardSet ScriptableObject with sprite arrays
 * - Container must have GridLayoutGroup component
 * - Card prefab must have Card.cs script
 */