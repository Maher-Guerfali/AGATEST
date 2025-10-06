using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Score UI")]
    public TMP_Text scoreText;
    public TMP_Text comboText;

   

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TMP_Text finalScoreText;
    public Button restartButton;
    public Button menuButton;
    public Button quitButton;

    [Header("Game Controls")]
    public Button newGameButton;
    public Button saveGameButton;
    public Button loadGameButton;
    public Button returnToMenuButton;

  

    [Header("Status Messages")]
    public GameObject messagePanel;
    public TMP_Text messageText;

    void Start()
    {
        InitializeUI();
        SetupButtonListeners();
    }

    private void InitializeUI()
    {
        // Hide panels at start
        if (gameOverPanel)
            gameOverPanel.SetActive(false);

        

        if (messagePanel)
            messagePanel.SetActive(false);

        // Initialize score display
        UpdateScore(0, 0);
    }

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
    }

    #region Score and Display Updates

    public void UpdateScore(int score, int combo)
    {
        if (scoreText)
            scoreText.text = $"Score: {score:N0}"; // Format with commas for large numbers

        if (comboText)
        {
            if (combo > 0)
                comboText.text = $"Combo: x{combo}";
            else
                comboText.text = "Combo: --";
        }
    }

  

   

    private IEnumerator AnimateMatchEffect(GameObject effect)
    {
        if (!effect) yield break;

        RectTransform rect = effect.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = effect.GetComponent<CanvasGroup>();

        if (!canvasGroup)
            canvasGroup = effect.AddComponent<CanvasGroup>();

        Vector3 startPos = rect.anchoredPosition;
        Vector3 endPos = startPos + Vector3.up * 100f; // Move up 100 pixels

        float elapsed = 0f;

       
    }

    private IEnumerator ComboFlash()
    {
        if (!comboText) yield break;

        Color originalColor = comboText.color;
        Color flashColor = Color.yellow;

        // Flash effect
        for (int i = 0; i < 3; i++)
        {
            comboText.color = flashColor;
            yield return new WaitForSeconds(0.1f);
            comboText.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }
    }

    #endregion

    #region Preview System

    public void ShowPreview()
    {
       

        SetInteractable(false);
    }

   


    #endregion

    #region Game Over

    public void ShowGameOver(int finalScore)
    {
        if (gameOverPanel)
            gameOverPanel.SetActive(true);

        // Update final score display
        UpdateScore(finalScore, GameManager.Instance.combo);

        if (finalScoreText)
            finalScoreText.text = $"Final Score: {finalScore:N0}";

        Debug.Log($"Game Over! Final Score: {finalScore}");
    }

    public void HideGameOver()
    {
        if (gameOverPanel)
            gameOverPanel.SetActive(false);
    }

    #endregion

    #region Button Handlers

    public void RestartGame()
    {
        Debug.Log("Restart button clicked");
        HideGameOver();

        if (GameManager.Instance)
            GameManager.Instance.StartNewGameWithPreview();

        PlayButtonSound();
    }

    public void StartNewGame()
    {
        Debug.Log("New Game button clicked");
        HideGameOver();

        if (GameManager.Instance)
            GameManager.Instance.StartNewGameWithPreview();

        PlayButtonSound();
    }

    public void SaveGame()
    {
        Debug.Log("Save Game button clicked");

        if (GameManager.Instance && GameManager.Instance.IsGameStarted)
        {
            GameState state = GameManager.Instance.CaptureState();
            SaveSystem.Save(state);
            ShowTemporaryMessage("Game Saved!", 2f);
        }
        else
        {
            ShowTemporaryMessage("No active game to save!", 2f);
        }

        PlayButtonSound();
    }

    public void LoadGame()
    {
        Debug.Log("Load Game button clicked");

        if (!SaveSystem.HasSaveFile())
        {
            ShowTemporaryMessage("No saved game found!", 2f);
            PlayButtonSound();
            return;
        }

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

    public void ReturnToMenu()
    {
        Debug.Log("Return to Menu button clicked");
        HideGameOver();

        if (GameManager.Instance)
            GameManager.Instance.ReturnToMenu();

        PlayButtonSound();
    }

    public void QuitGame()
    {
        Debug.Log("Quit button clicked");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region Helper Methods

    public void ShowTemporaryMessage(string message, float duration)
    {
        StartCoroutine(ShowTemporaryMessageCoroutine(message, duration));
    }

    private IEnumerator ShowTemporaryMessageCoroutine(string message, float duration)
    {
        // Show message
        if (messagePanel)
            messagePanel.SetActive(true);

        if (messageText)
            messageText.text = message;

        Debug.Log(message);

        yield return new WaitForSeconds(duration);

        // Hide message
        if (messagePanel)
            messagePanel.SetActive(false);
    }

    private void PlayButtonSound()
    {
        if (SoundManager.Instance)
            SoundManager.Instance.PlayFlip();
    }

    #endregion

    #region Public Utility Methods

    public void SetInteractable(bool interactable)
    {
        // Enable/disable game control buttons during gameplay
        if (newGameButton) newGameButton.interactable = interactable;
        if (saveGameButton) saveGameButton.interactable = interactable;
        if (loadGameButton) loadGameButton.interactable = interactable;
        if (returnToMenuButton) returnToMenuButton.interactable = interactable;
    }

    public void UpdateLoadButtonState()
    {
        if (loadGameButton)
        {
            loadGameButton.interactable = SaveSystem.HasSaveFile();
        }
    }

    #endregion

    #region Editor Utilities

#if UNITY_EDITOR
    [ContextMenu("Test Show Message")]
    private void TestShowMessage()
    {
        ShowTemporaryMessage("Test Message!", 3f);
    }

    [ContextMenu("Test Game Over")]
    private void TestGameOver()
    {
        ShowGameOver(12345);
    }
#endif

    #endregion
}