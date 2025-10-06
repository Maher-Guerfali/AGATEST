using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public GridBuilder gridBuilder;
    public Card cardPrefab;
    public RectTransform gridContainer;
    [Min(1)] public int rows = 4;
    [Min(1)] public int cols = 4;
    [Range(0.1f, 2f)] public float revealDelay = 0.8f;
    [Min(2)] public int pairSize = 2;

    [Header("Preview Settings")]
    [SerializeField] private float previewDuration = 3f;
    [SerializeField] private bool enablePreview = true;

    [Header("Score")]
    public int score = 0;
    public int combo = 0;
    public UIManager uiManager;

    [Header("Menu Integration")]
    public MenuManager menuManager;

    // Gameplay state
    private readonly Queue<Card> compareQueue = new Queue<Card>();
    private bool comparing = false;
    private bool gameStarted = false;
    private bool previewActive = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Don't auto-start the game - let the menu handle it
        if (menuManager == null)
        {
            StartNewGame(); // Fallback for testing without menu
        }
    }

    public void StartNewGame()
    {
        score = 0;
        combo = 0;
        gameStarted = false;
        previewActive = false;
        uiManager?.UpdateScore(score, combo);

        // Clear any pending comparisons
        compareQueue.Clear();
        comparing = false;

        gridBuilder.BuildGrid(cardPrefab, gridContainer);
        gameStarted = true;
    }

    /// <summary>
    /// Starts a new game with a 3-second preview of all cards
    /// </summary>
    public void StartNewGameWithPreview()
    {
        score = 0;
        combo = 0;
        gameStarted = false;
        previewActive = true;
        uiManager?.UpdateScore(score, combo);

        // Clear any pending comparisons
        compareQueue.Clear();
        comparing = false;

        gridBuilder.BuildGrid(cardPrefab, gridContainer);

        if (enablePreview)
        {
            StartCoroutine(ShowPreviewThenStart());
        }
        else
        {
            gameStarted = true;
            previewActive = false;
        }
    }

    private IEnumerator ShowPreviewThenStart()
    {
        Debug.Log("Starting card preview...");

        // Get all cards and reveal them
        var allCards = FindObjectsOfType<Card>();
        foreach (var card in allCards)
        {
            if (!card.IsRevealed)
            {
                card.Reveal();
            }
        }

        // Show preview message if UI supports it
        uiManager?.SetInteractable(false);

        // Wait for preview duration
        yield return new WaitForSeconds(previewDuration);

        Debug.Log("Preview ended, hiding cards...");

        // Hide all cards
        foreach (var card in allCards)
        {
            if (!card.IsMatched)
            {
                card.HideInstant();
            }
        }

        // Enable game interaction
        previewActive = false;
        gameStarted = true;
        uiManager?.SetInteractable(true);

        Debug.Log("Game started!");
    }

    public void RegisterFlip(Card card)
    {
        // Don't register flips during preview or before game starts
        if (!gameStarted || previewActive)
        {
            Debug.Log($"RegisterFlip ignored - game not started or preview active");
            return;
        }

        Debug.Log($"RegisterFlip called for card {card.faceId}. IsRevealed: {card.IsRevealed}, IsMatched: {card.IsMatched}");

        // Only register if card is actually revealed and not matched
        if (!card.IsRevealed || card.IsMatched)
        {
            Debug.Log($"Card {card.faceId} registration skipped - not revealed or already matched");
            return;
        }

        // Add the flipped card to the queue
        compareQueue.Enqueue(card);
        Debug.Log($"Card {card.faceId} added to queue. Queue count: {compareQueue.Count}");

        // Start comparison routine if not already running
        if (!comparing)
        {
            Debug.Log("Starting comparison process");
            StartCoroutine(ProcessComparisons());
        }
    }

    private IEnumerator ProcessComparisons()
    {
        comparing = true;

        while (compareQueue.Count >= pairSize)
        {
            Debug.Log($"Processing comparison with {compareQueue.Count} cards in queue");

            // Collect group of flipped cards
            var group = new List<Card>();
            for (int i = 0; i < pairSize; i++)
            {
                if (compareQueue.Count > 0)
                    group.Add(compareQueue.Dequeue());
            }

            Debug.Log($"Collected {group.Count} cards for comparison");

            // Skip if invalid (already matched or flipped back)
            if (group.Any(c => c.IsMatched || !c.IsRevealed))
            {
                Debug.Log("Skipping group - some cards are matched or not revealed");
                continue;
            }

            // Small delay for visuals
            yield return new WaitForSeconds(0.15f);

            // Check match
            bool isMatch = IsMatch(group);
            Debug.Log($"Match result: {isMatch} for cards with IDs: {string.Join(", ", group.Select(c => c.faceId))}");

            if (isMatch)
            {
                // MATCH - Mark all cards as matched
                foreach (var c in group)
                    c.MarkMatched();

                combo++;
                int gainedPoints = 100 * combo;
                score += gainedPoints;

                Debug.Log($"Match! Score: {score}, Combo: {combo}");

                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlayMatch();

               
            }
            else
            {
                // MISMATCH - Reset combo and hide cards after delay
                combo = 0;
                Debug.Log($"Mismatch! Cards will flip back. Combo reset to 0");

                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlayMismatch();

                // Use the proper HideAfterMismatch method
                foreach (var c in group)
                {
                    if (!c.IsMatched)
                        c.HideAfterMismatch();
                }
            }

            // Update score after each check
            uiManager?.UpdateScore(score, combo);

            // Check end condition
            if (gridBuilder.AllMatched())
            {
                Debug.Log("All cards matched! Game over.");
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlayGameOver();
                uiManager?.ShowGameOver(score);
                gameStarted = false; // Prevent further interactions
                break;
            }
        }

        comparing = false;
        Debug.Log("Comparison process ended");
    }

    private bool IsMatch(List<Card> group)
    {
        if (group.Count == 0) return false;

        int id = group[0].faceId;
        bool match = group.All(c => c.faceId == id);

        Debug.Log($"Checking match for {group.Count} cards. Reference ID: {id}, All match: {match}");
        return match;
    }

    // --- Game State Saving & Loading ---
    public GameState CaptureState()
    {
        return gridBuilder.CaptureState(score, combo);
    }

    // Replace the LoadState method in your GameManager with this updated version:

    public void LoadState(GameState state)
    {
        if (state == null)
        {
            Debug.LogError("Cannot load null game state");
            return;
        }

        Debug.Log($"Loading game state: Score={state.score}, Combo={state.combo}, Cards={state.cards?.Count ?? 0}");

        // Clear any existing game state
        compareQueue.Clear();
        comparing = false;

        // Update the game settings to match saved state
        rows = state.rows;
        cols = state.cols;

        // Update GridBuilder settings
        if (gridBuilder != null && gridBuilder.settings != null)
        {
            gridBuilder.settings.rows = state.rows;
            gridBuilder.settings.cols = state.cols;
        }

        // Set score and combo
        this.score = state.score;
        this.combo = state.combo;

        // Update UI immediately
        uiManager?.UpdateScore(this.score, this.combo);

        // FIRST: Build the grid with cards, THEN restore their states
        Debug.Log("Building grid for loaded game...");
        gridBuilder.BuildGrid(cardPrefab, gridContainer);

        // SECOND: Apply the saved states to the newly created cards
        Debug.Log("Restoring card states...");
        gridBuilder.RestoreState(state);

        // Set game state flags AFTER everything is built
        gameStarted = true;
        previewActive = false;

        Debug.Log($"Game state loaded successfully. Final score: {this.score}, combo: {this.combo}");
    }
    // --- Public Properties for UI ---
    public bool IsGameStarted => gameStarted;
    public bool IsPreviewActive => previewActive;

    // --- Menu Integration Methods ---
    public void ReturnToMenu()
    {
        gameStarted = false;
        previewActive = false;
        comparing = false;
        compareQueue.Clear();

        if (menuManager != null)
        {
            menuManager.ReturnToMenu();
        }
    }

    // --- Preview Control Methods ---
    public void SetPreviewEnabled(bool enabled)
    {
        enablePreview = enabled;
    }

    public void SetPreviewDuration(float duration)
    {
        previewDuration = Mathf.Clamp(duration, 1f, 10f);
    }

    // --- Debug Methods ---
#if UNITY_EDITOR
    [ContextMenu("Start Game With Preview")]
    private void DebugStartWithPreview()
    {
        StartNewGameWithPreview();
    }

    [ContextMenu("Start Game Without Preview")]
    private void DebugStartWithoutPreview()
    {
        StartNewGame();
    }

    [ContextMenu("Force End Preview")]
    private void DebugEndPreview()
    {
        if (previewActive)
        {
            StopAllCoroutines();
            previewActive = false;
            gameStarted = true;

            var allCards = FindObjectsOfType<Card>();
            foreach (var card in allCards)
            {
                if (!card.IsMatched)
                {
                    card.HideInstant();
                }
            }
        }
    }
#endif
}