using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages all UI elements, button interactions, and visual feedback for the game.
/// Serves as the bridge between game logic and user interface.
/// 
/// RESPONSIBILITIES:
/// - Update score and combo displays
/// - Show/hide game over screen
/// - Handle all button clicks (New Game, Save, Load, etc.)
/// - Display temporary status messages
/// - Enable/disable UI during gameplay states
/// - Provide audio feedback for interactions
/// 
/// UI STRUCTURE:
/// - Score Panel: Real-time score and combo display
/// - Game Over Panel: Final score and restart options
/// - Control Buttons: New Game, Save, Load, Return to Menu
/// - Message Panel: Temporary notifications (saved, loaded, errors)
/// 
/// Author: [Your Team Name]
/// Last Modified: 2025
/// </summary>
public class UIManager : MonoBehaviour
{
    #region Inspector References - Score Display

    [Header("Score UI")]
    [Tooltip("Text displaying current score with formatting (e.g., 'Score: 1,234')")]
    public TMP_Text scoreText;

    [Tooltip("Text displaying current combo multiplier (e.g., 'Combo: x5' or 'Combo: --')")]
    public TMP_Text comboText;

    #endregion

    #region Inspector References - Game Over Panel

    [Header("Game Over UI")]
    [Tooltip("Panel shown when all cards are matched")]
    public GameObject gameOverPanel;

    [Tooltip("Text displaying final score on game over screen")]
    public TMP_Text finalScoreText;

    [Tooltip("Button to restart game with new shuffle")]
    public Button restartButton;

    [Tooltip("Button to return to main menu")]
    public Button menuButton;

    [Tooltip("Button to quit application")]
    public Button quitButton;

    #endregion

    #region Inspector References - Game Controls

    [Header("Game Controls")]
    [Tooltip("Button to start a new game (discards current progress)")]
    public Button newGameButton;

    [Tooltip("Button to save current game state")]
    public Button saveGameButton;

    [Tooltip("Button to load previously saved game")]
    public Button loadGameButton;

    [Tooltip("Button to return to menu without saving")]
    public Button returnToMenuButton;

    #endregion

    #region Inspector References - Status Messages

    [Header("Status Messages")]
    [Tooltip("Panel for displaying temporary messages")]
    public GameObject messagePanel;

    [Tooltip("Text component for message content")]
    public TMP_Text messageText;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Called on first frame. Initializes UI state and sets up button listeners.
    /// </summary>
    void Start()
    {
        InitializeUI();
        SetupButtonListeners();
    }

    /// <summary>
    /// Initializes all UI elements to their default state.
    /// Hides panels, resets displays, and prepares for gameplay.
    /// </summary>
    private void InitializeUI()
    {
        // Hide panels at start
        if (gameOverPanel)
            gameOverPanel.SetActive(false);

        if (messagePanel)
            messagePanel.SetActive(false);

        // Initialize score display with zeros
        UpdateScore(0, 0);

        Debug.Log("[UIManager] UI initialized");
    }

    /// <summary>
    /// Sets up click listeners for all buttons.
    /// Connects UI buttons to their corresponding handler methods.
    /// </summary>
    private void SetupButtonListeners()
    {
        // Game Over Panel Buttons
        if (restartButton)
            restartButton.onClick.AddListener(RestartGame);

        if (menuButton)
            menuButton.onClick.AddListener(ReturnToMenu);

        if (quitButton)
            quitButton.onClick.AddListener(QuitGame);

        // Game Control Buttons
        if (newGameButton)
            newGameButton.onClick.AddListener(StartNewGame);

        if (saveGameButton)
            saveGameButton.onClick.AddListener(SaveGame);

        if (loadGameButton)
            loadGameButton.onClick.AddListener(LoadGame);

        if (returnToMenuButton)
            returnToMenuButton.onClick.AddListener(ReturnToMenu);

        Debug.Log("[UIManager] Button listeners configured");
    }

    #endregion

    #region Score and Display Updates

    /// <summary>
    /// Updates the score and combo display with formatted text.
    /// Called by GameManager after each card comparison.
    /// 
    /// FORMATTING:
    /// - Score: Comma-separated thousands (e.g., "Score: 12,345")
    /// - Combo: Shows "x{combo}" when active, "--" when zero
    /// 
    /// Example displays:
    /// - "Score: 0" and "Combo: --" (initial)
    /// - "Score: 100" and "Combo: x1" (first match)
    /// - "Score: 1,500" and "Combo: x5" (five consecutive matches)
    /// </summary>
    /// <param name="score">Current game score</param>
    /// <param name="combo">Current combo multiplier</param>
    public void UpdateScore(int score, int combo)
    {
        if (scoreText)
        {
            // Format with commas for large numbers (e.g., 1,234,567)
            scoreText.text = $"Score: {score:N0}";
        }

        if (comboText)
        {
            if (combo > 0)
                comboText.text = $"Combo: x{combo}";
            else
                comboText.text = "Combo: --";
        }

        // Debug.Log($"[UIManager] Score updated: {score}, Combo: x{combo}");
    }

    #endregion

    #region Game Over

    /// <summary>
    /// Displays the game over screen with final score.
    /// Called by GameManager when all cards are matched.
    /// 
    /// DISPLAY:
    /// - Shows game over panel
    /// - Updates final score text with formatting
    /// - Keeps score/combo visible on main UI
    /// - Enables restart/menu/quit buttons
    /// </summary>
    /// <param name="finalScore">Final game score to display</param>
    public void ShowGameOver(int finalScore)
    {
        if (gameOverPanel)
            gameOverPanel.SetActive(true);

        // Keep the main score display updated
        UpdateScore(finalScore, GameManager.Instance.combo);

        // Also show in game over panel
        if (finalScoreText)
            finalScoreText.text = $"Final Score: {finalScore:N0}";

        Debug.Log($"[UIManager] Game Over! Final Score: {finalScore}");
    }

    /// <summary>
    /// Hides the game over panel.
    /// Called when restarting game or returning to menu.
    /// </summary>
    public void HideGameOver()
    {
        if (gameOverPanel)
            gameOverPanel.SetActive(false);

        Debug.Log("[UIManager] Game over panel hidden");
    }

    #endregion

    #region Button Handlers

    /// <summary>
    /// Handles Restart button click.
    /// Starts a new game with preview from game over screen.
    /// 
    /// SEQUENCE:
    /// 1. Hide game over panel
    /// 2. Call GameManager.StartNewGameWithPreview()
    /// 3. Play button sound
    /// </summary>
    public void RestartGame()
    {
        Debug.Log("[UIManager] Restart button clicked");
        HideGameOver();

        if (GameManager.Instance)
            GameManager.Instance.StartNewGameWithPreview();

        PlayButtonSound();
    }

    /// <summary>
    /// Handles New Game button click.
    /// Starts a fresh game with preview, discarding current progress.
    /// 
    /// WARNING: Does not save current game. Player will lose progress.
    /// Consider adding confirmation dialog for active games.
    /// </summary>
    public void StartNewGame()
    {
        Debug.Log("[UIManager] New Game button clicked");
        HideGameOver();

        if (GameManager.Instance)
            GameManager.Instance.StartNewGameWithPreview();

        PlayButtonSound();
    }

    /// <summary>
    /// Handles Save Game button click.
    /// Captures current game state and saves to disk.
    /// 
    /// VALIDATION:
    /// - Checks if game is started (can't save before game begins)
    /// - Shows success/error message to player
    /// 
    /// SAVED DATA:
    /// - Grid dimensions (rows, cols)
    /// - Score and combo
    /// - All card states (faceId, matched, revealed)
    /// 
    /// FILE LOCATION: Application.persistentDataPath/cardgame_save.json
    /// </summary>
    public void SaveGame()
    {
        Debug.Log("[UIManager] Save Game button clicked");

        if (GameManager.Instance && GameManager.Instance.IsGameStarted)
        {
            GameState state = GameManager.Instance.CaptureState();
            SaveSystem.Save(state);
            ShowTemporaryMessage("Game Saved!", 2f);

            // Update load button state (now there's a save file)
            UpdateLoadButtonState();
        }
        else
        {
            ShowTemporaryMessage("No active game to save!", 2f);
        }

        PlayButtonSound();
    }

    /// <summary>
    /// Handles Load Game button click.
    /// Loads previously saved game state from disk.
    /// 
    /// VALIDATION:
    /// - Checks if save file exists
    /// - Validates loaded data is not corrupted
    /// 
    /// SEQUENCE:
    /// 1. Check for save file
    /// 2. Load GameState from JSON
    /// 3. Call GameManager.LoadState()
    /// 4. Hide game over panel if showing
    /// 5. Show success/error message
    /// 
    /// OVERWRITES: Current game progress (no confirmation dialog yet)
    /// </summary>
    public void LoadGame()
    {
        Debug.Log("[UIManager] Load Game button clicked");

        // Check if save file exists
        if (!SaveSystem.HasSaveFile())
        {
            ShowTemporaryMessage("No saved game found!", 2f);
            PlayButtonSound();
            return;
        }

        // Load the saved state
        GameState state = SaveSystem.Load();
        if (state != null && GameManager.Instance)
        {
            GameManager.Instance.LoadState(state);
            HideGameOver();
            ShowTemporaryMessage("Game Loaded!", 2f);
        }
        else
        {
            ShowTemporaryMessage("Failed to load game!", 2f);
        }

        PlayButtonSound();
    }

    /// <summary>
    /// Handles Return to Menu button click.
    /// Returns player to main menu, discarding unsaved progress.
    /// 
    /// SEQUENCE:
    /// 1. Hide game over panel
    /// 2. Call GameManager.ReturnToMenu()
    /// 3. GameManager notifies MenuManager to change scene
    /// 
    /// WARNING: Does not auto-save. Consider adding save prompt.
    /// </summary>
    public void ReturnToMenu()
    {
        Debug.Log("[UIManager] Return to Menu button clicked");
        HideGameOver();

        if (GameManager.Instance)
            GameManager.Instance.ReturnToMenu();

        PlayButtonSound();
    }

    /// <summary>
    /// Handles Quit button click.
    /// Exits the application.
    /// 
    /// BEHAVIOR:
    /// - In Editor: Stops play mode
    /// - In Build: Closes application
    /// 
    /// NOTE: Does not save progress. Add save prompt for production.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[UIManager] Quit button clicked");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Displays a temporary message that auto-hides after duration.
    /// Used for status notifications (saved, loaded, errors).
    /// 
    /// USAGE:
    /// ShowTemporaryMessage("Game Saved!", 2f); // Shows for 2 seconds
    /// </summary>
    /// <param name="message">Message text to display</param>
    /// <param name="duration">How long to show message in seconds</param>
    public void ShowTemporaryMessage(string message, float duration)
    {
        StartCoroutine(ShowTemporaryMessageCoroutine(message, duration));
    }

    /// <summary>
    /// Coroutine that handles the message display sequence.
    /// Shows message, waits, then hides.
    /// </summary>
    private IEnumerator ShowTemporaryMessageCoroutine(string message, float duration)
    {
        // Show message
        if (messagePanel)
            messagePanel.SetActive(true);

        if (messageText)
            messageText.text = message;

        Debug.Log($"[UIManager] Showing message: {message}");

        yield return new WaitForSeconds(duration);

        // Hide message
        if (messagePanel)
            messagePanel.SetActive(false);
    }

    /// <summary>
    /// Plays UI button click sound.
    /// Called after each button interaction for audio feedback.
    /// </summary>
    private void PlayButtonSound()
    {
        if (SoundManager.Instance)
            SoundManager.Instance.PlayFlip();
    }

    #endregion

    #region Public Utility Methods

    /// <summary>
    /// Enables or disables all game control buttons.
    /// Used during preview to prevent player interaction.
    /// 
    /// AFFECTED BUTTONS:
    /// - New Game
    /// - Save Game
    /// - Load Game
    /// - Return to Menu
    /// 
    /// NOT AFFECTED:
    /// - Game over panel buttons (they're hidden anyway)
    /// - Card interactions (handled by Card.cs and GameManager)
    /// 
    /// USAGE:
    /// uiManager.SetInteractable(false); // Disable during preview
    /// uiManager.SetInteractable(true);  // Enable when game starts
    /// </summary>
    /// <param name="interactable">True to enable buttons, false to disable</param>
    public void SetInteractable(bool interactable)
    {
        // Enable/disable game control buttons during gameplay
        if (newGameButton) newGameButton.interactable = interactable;
        if (saveGameButton) saveGameButton.interactable = interactable;
        if (loadGameButton) loadGameButton.interactable = interactable;
        if (returnToMenuButton) returnToMenuButton.interactable = interactable;

        Debug.Log($"[UIManager] UI interactable set to: {interactable}");
    }

    /// <summary>
    /// Updates the Load button's interactable state based on save file existence.
    /// Disables Load button if no save file exists.
    /// Call this after saving or on game start.
    /// 
    /// PREVENTS: Clicking Load when no save exists
    /// IMPROVES: User experience by visual feedback
    /// </summary>
    public void UpdateLoadButtonState()
    {
        if (loadGameButton)
        {
            bool hasSave = SaveSystem.HasSaveFile();
            loadGameButton.interactable = hasSave;

            Debug.Log($"[UIManager] Load button {(hasSave ? "enabled" : "disabled")} (save file {(hasSave ? "exists" : "not found")})");
        }
    }

    #endregion

    #region Editor Utilities

#if UNITY_EDITOR
    /// <summary>
    /// Debug method: Test message display.
    /// Right-click UIManager in Inspector > Test Show Message
    /// </summary>
    [ContextMenu("Test Show Message")]
    private void TestShowMessage()
    {
        ShowTemporaryMessage("Test Message!", 3f);
    }

    /// <summary>
    /// Debug method: Test game over screen.
    /// Right-click UIManager in Inspector > Test Game Over
    /// </summary>
    [ContextMenu("Test Game Over")]
    private void TestGameOver()
    {
        ShowGameOver(12345);
    }

    /// <summary>
    /// Debug method: Test UI disable.
    /// Right-click UIManager in Inspector > Test Disable UI
    /// </summary>
    [ContextMenu("Test Disable UI")]
    private void TestDisableUI()
    {
        SetInteractable(false);
    }

    /// <summary>
    /// Debug method: Test UI enable.
    /// Right-click UIManager in Inspector > Test Enable UI
    /// </summary>
    [ContextMenu("Test Enable UI")]
    private void TestEnableUI()
    {
        SetInteractable(true);
    }
#endif

    #endregion
}

/*
 * USAGE EXAMPLES:
 * 
 * 1. Update score display:
 *    uiManager.UpdateScore(500, 3); // Score: 500, Combo: x3
 * 
 * 2. Show game over:
 *    uiManager.ShowGameOver(1234); // Final Score: 1,234
 * 
 * 3. Disable UI during preview:
 *    uiManager.SetInteractable(false);
 * 
 * 4. Show temporary message:
 *    uiManager.ShowTemporaryMessage("Game Saved!", 2f);
 * 
 * 5. Update load button on start:
 *    void Start() {
 *        uiManager.UpdateLoadButtonState();
 *    }
 * 
 * SETUP CHECKLIST:
 * 
 * 1. Create Canvas with:
 *    - Score Panel with TMP_Text components
 *    - Game Over Panel (inactive by default)
 *    - Control buttons (New Game, Save, Load, Menu)
 *    - Message Panel (inactive by default)
 * 
 * 2. Assign references in Inspector:
 *    - Drag all text components to corresponding fields
 *    - Drag all buttons to button fields
 *    - Drag panels to panel fields
 * 
 * 3. Ensure TextMeshPro is imported:
 *    Window > TextMeshPro > Import TMP Essential Resources
 * 
 * BUTTON EVENT SETUP:
 * 
 * Two ways to connect buttons:
 * 
 * METHOD 1: Inspector (Not Recommended - use code)
 * - Select button in hierarchy
 * - In Button component, add onClick event
 * - Drag UIManager GameObject
 * - Select method from dropdown
 * 
 * METHOD 2: Code (Recommended - already done in SetupButtonListeners)
 * - Assign button references in Inspector
 * - SetupButtonListeners() automatically connects them
 * - Easier to maintain and debug
 * 
 * UI LAYOUT TIPS:
 * 
 * Score Panel:
 * - Anchor: Top
 * - Position: Center-top of screen
 * - Layout: Horizontal or Vertical
 * 
 * Game Over Panel:
 * - Anchor: Center
 * - Full screen overlay with semi-transparent background
 * - Vertical layout with buttons at bottom
 * 
 * Control Buttons:
 * - Anchor: Bottom-right or top-right
 * - Vertical layout for stacked buttons
 * - Consider using button groups for visual hierarchy
 * 
 * Message Panel:
 * - Anchor: Top-center or bottom-center
 * - Auto-hide after duration
 * - Semi-transparent background for readability
 * 
 * INTEGRATION WITH GAMEMANAGER:
 * 
 * GameManager calls UIManager methods:
 * - ProcessComparisons() → UpdateScore()
 * - AllMatched() → ShowGameOver()
 * - StartNewGameWithPreview() → SetInteractable(false/true)
 * 
 * UIManager calls GameManager methods:
 * - StartNewGame() → GameManager.StartNewGameWithPreview()
 * - SaveGame() → GameManager.CaptureState()
 * - LoadGame() → GameManager.LoadState()
 * - ReturnToMenu() → GameManager.ReturnToMenu()
 * 
 * RECOMMENDED IMPROVEMENTS:
 * 
 * 1. Confirmation Dialogs:
 *    - Add "Are you sure?" before starting new game with active progress
 *    - Add "Save before quitting?" dialog
 * 
 * 2. Animated Transitions:
 *    - Fade in/out for panels
 *    - Scale animation for score changes
 *    - Particle effects for high combos
 * 
 * 3. Sound Feedback:
 *    - Different sounds for different buttons
 *    - Success/error sounds for save/load
 *    - Combo milestone sounds
 * 
 * 4. Score Animation:
 *    - Count up animation for score changes
 *    - Flash effect for combo increases
 *    - Shake effect for combo breaks
 * 
 * 5. Loading Indicator:
 *    - Show spinner during load operations
 *    - Prevent button spam during operations
 */