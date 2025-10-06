using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GridBuilder : MonoBehaviour
{
    [Header("Data")]
    public CardSet cardSet;       // ScriptableObject with front/back sprites
    public GameSettings settings; // rows, cols, pairSize, etc.

    [Header("Card Sizing")]
    [SerializeField] private Vector2 defaultCardSize = new Vector2(190f, 240f);
    [SerializeField] private Vector2 minCardSize = new Vector2(100f, 100f);
    [SerializeField] private float paddingPercentage = 0.1f; // 10% padding between cards

    private List<Card> cards = new List<Card>();

    /// <summary>
    /// Builds a new grid of cards inside the given container using CardSet sprites.
    /// </summary>
    public void BuildGrid(Card cardPrefab, RectTransform container)
    {
        ClearGrid();
        int total = settings.rows * settings.cols;

        if (total % settings.pairSize != 0)
            Debug.LogWarning("Grid size not divisible by pairSize, some cards may not match.");

        // Calculate optimal card size for current grid
        Vector2 cardSize = CalculateCardSize(container);
        Debug.Log($"Calculated card size for {settings.rows}x{settings.cols} grid: {cardSize}");

        // Setup GridLayoutGroup with calculated size
        SetupGridLayout(container, cardSize);

        // --- Prepare deck
        List<int> poolIds = new List<int>();
        int groupCount = total / settings.pairSize;

        for (int i = 0; i < groupCount; i++)
        {
            for (int j = 0; j < settings.pairSize; j++)
                poolIds.Add(i); // assign same ID for pair/triplet
        }

        Shuffle(poolIds);

        // --- Instantiate cards
        for (int i = 0; i < total; i++)
        {
            var c = Instantiate(cardPrefab, container);
            int id = poolIds[i];
            Sprite frontSprite = cardSet.frontSprites[id % cardSet.frontSprites.Count];
            c.Initialize(id, frontSprite, cardSet.backSprite);

            // Set card size
            RectTransform cardRect = c.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.sizeDelta = cardSize;
            }

            cards.Add(c);
        }
    }

    /// <summary>
    /// Calculates optimal card size based on container size and grid dimensions
    /// </summary>
    private Vector2 CalculateCardSize(RectTransform container)
    {
        // Get container dimensions
        Vector2 containerSize = container.rect.size;

        // If container size is not available yet, use a default reference size
        if (containerSize.x <= 0 || containerSize.y <= 0)
        {
            containerSize = new Vector2(1080f, 1920f); // Common mobile portrait reference
            Debug.LogWarning("Container size not available, using default reference size");
        }

        // Calculate available space per card (accounting for padding)
        float availableWidth = containerSize.x * (1f - paddingPercentage);
        float availableHeight = containerSize.y * (1f - paddingPercentage);

        float cardWidth = availableWidth / settings.cols;
        float cardHeight = availableHeight / settings.rows;

        // Apply your specific sizing rules
        Vector2 calculatedSize;

        // For small grids (up to 3 rows, 5 columns), use default size or smaller
        if (settings.rows <= 3 && settings.cols <= 5)
        {
            calculatedSize = new Vector2(
                Mathf.Min(defaultCardSize.x, cardWidth),
                Mathf.Min(defaultCardSize.y, cardHeight)
            );
        }
        // For larger grids (6+ rows, 5+ columns), use smaller square cards
        else if (settings.rows >= 6 || settings.cols >= 6)
        {
            float squareSize = Mathf.Min(cardWidth, cardHeight);
            squareSize = Mathf.Min(squareSize, 130f); // Your specified max size for large grids
            calculatedSize = new Vector2(squareSize, squareSize);
        }
        // For medium grids, interpolate between default and small
        else
        {
            float factor = Mathf.Max(settings.rows, settings.cols) / 6f; // Scale factor based on grid size
            float width = Mathf.Lerp(defaultCardSize.x, 130f, factor);
            float height = Mathf.Lerp(defaultCardSize.y, 130f, factor);

            calculatedSize = new Vector2(
                Mathf.Min(width, cardWidth),
                Mathf.Min(height, cardHeight)
            );
        }

        // Ensure minimum size
        calculatedSize.x = Mathf.Max(calculatedSize.x, minCardSize.x);
        calculatedSize.y = Mathf.Max(calculatedSize.y, minCardSize.y);

        return calculatedSize;
    }

    /// <summary>
    /// Sets up the GridLayoutGroup component with the calculated card size and spacing
    /// </summary>
    private void SetupGridLayout(RectTransform container, Vector2 cardSize)
    {
        var layout = container.GetComponent<GridLayoutGroup>();
        if (layout != null)
        {
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = settings.cols;
            layout.cellSize = cardSize;

            // Calculate spacing based on card size and container
            Vector2 containerSize = container.rect.size;
            if (containerSize.x > 0 && containerSize.y > 0)
            {
                float spacingX = (containerSize.x - (cardSize.x * settings.cols)) / (settings.cols + 1);
                float spacingY = (containerSize.y - (cardSize.y * settings.rows)) / (settings.rows + 1);

                spacingX = Mathf.Max(5f, spacingX); // Minimum 5px spacing
                spacingY = Mathf.Max(5f, spacingY);

                layout.spacing = new Vector2(spacingX, spacingY);
                layout.padding = new RectOffset(
                    Mathf.RoundToInt(spacingX * 0.5f),
                    Mathf.RoundToInt(spacingX * 0.5f),
                    Mathf.RoundToInt(spacingY * 0.5f),
                    Mathf.RoundToInt(spacingY * 0.5f)
                );
            }
            else
            {
                // Default spacing if container size not available
                float defaultSpacing = cardSize.x * 0.05f; // 5% of card width
                layout.spacing = new Vector2(defaultSpacing, defaultSpacing);
                layout.padding = new RectOffset(10, 10, 10, 10);
            }

            // Fit the content
            layout.childAlignment = TextAnchor.MiddleCenter;
        }
    }

    /// <summary>
    /// Public method to update grid size and rebuild with new card sizes
    /// </summary>
    public void UpdateGridSize(int newRows, int newCols)
    {
        settings.rows = newRows;
        settings.cols = newCols;

        if (GameManager.Instance != null)
        {
            BuildGrid(GameManager.Instance.cardPrefab, GameManager.Instance.gridContainer);
        }
    }

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
        return s;
    }

    // Replace the RestoreState method in your GridBuilder with this updated version:

    // Replace the RestoreState method in your GridBuilder with this updated version:

    public void RestoreState(GameState state)
    {
        if (state == null || state.cards == null)
        {
            Debug.LogError("Cannot restore state: state or cards is null");
            return;
        }

        Debug.Log($"Restoring state for {cards.Count} cards, saved states: {state.cards.Count}");

        // If grid size doesn't match, we already rebuilt in LoadState, so cards should match
        if (state.cards.Count != cards.Count)
        {
            Debug.LogError($"Card count mismatch: Expected {state.cards.Count}, but have {cards.Count} cards");
            return;
        }

        // Apply saved states to the existing cards
        for (int i = 0; i < cards.Count && i < state.cards.Count; i++)
        {
            var cardState = state.cards[i];
            var card = cards[i];

            Debug.Log($"Restoring card {i}: faceId={cardState.faceId}, revealed={cardState.isRevealed}, matched={cardState.isMatched}");

            // Re-initialize the card with correct face ID and sprites
            Sprite frontSprite = cardSet.frontSprites[cardState.faceId % cardSet.frontSprites.Count];
            card.Initialize(cardState.faceId, frontSprite, cardSet.backSprite);

            // Apply the saved state - order matters!
            if (cardState.isMatched)
            {
                // For matched cards: first reveal them, then mark as matched
                card.Reveal(); // Show the front face
                card.MarkMatched(); // Mark as matched (should keep them visible and uninteractable)
                Debug.Log($"Card {i} restored as MATCHED and VISIBLE");
            }
            else if (cardState.isRevealed)
            {
                card.Reveal(); // Show the front face for currently revealed cards
                Debug.Log($"Card {i} restored as REVEALED");
            }
            else
            {
                card.HideInstant(); // Make sure it's face down
                Debug.Log($"Card {i} restored as HIDDEN");
            }
        }

        Debug.Log("Card states restored successfully");
    }

    public bool AllMatched()
    {
        return cards.All(c => c.IsMatched);
    }

    public void ClearGrid()
    {
        foreach (var c in cards)
        {
            if (c != null) Destroy(c.gameObject);
        }
        cards.Clear();
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }

    // Helper method for testing different grid sizes in editor
    [ContextMenu("Test Small Grid (2x2)")]
    public void TestSmallGrid()
    {
        UpdateGridSize(2, 2);
    }

    [ContextMenu("Test Large Grid (6x5)")]
    public void TestLargeGrid()
    {
        UpdateGridSize(6, 5);
    }
}