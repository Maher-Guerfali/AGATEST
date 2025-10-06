using System.Collections.Generic;
using UnityEngine;
using System.IO;



public static class SaveSystem
{
    private static readonly string SAVE_FILE = "cardgame_save.json";

    public static void Save(GameState gameState)
    {
        try
        {
            // Add validation before saving
            if (gameState == null)
            {
                Debug.LogError("Cannot save: GameState is null");
                return;
            }

            if (gameState.cards == null)
            {
                Debug.LogError("Cannot save: Cards list is null");
                return;
            }

            Debug.Log($"Saving game state: Score={gameState.score}, Combo={gameState.combo}, Cards={gameState.cards.Count}");

            string json = JsonUtility.ToJson(gameState, true);
            string filePath = Path.Combine(Application.persistentDataPath, SAVE_FILE);
            File.WriteAllText(filePath, json);

            Debug.Log($"Game saved successfully to: {filePath}");
            Debug.Log($"Saved JSON preview: {json.Substring(0, Mathf.Min(200, json.Length))}...");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save game: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }

    public static GameState Load()
    {
        try
        {
            string filePath = Path.Combine(Application.persistentDataPath, SAVE_FILE);
            if (!File.Exists(filePath))
            {
                Debug.Log("No save file found");
                return null;
            }

            string json = File.ReadAllText(filePath);
            Debug.Log($"Loading JSON preview: {json.Substring(0, Mathf.Min(200, json.Length))}...");

            GameState gameState = JsonUtility.FromJson<GameState>(json);

            // Validate loaded state
            if (gameState == null)
            {
                Debug.LogError("Failed to deserialize GameState");
                return null;
            }

            Debug.Log($"Game loaded successfully: Score={gameState.score}, Combo={gameState.combo}, Cards={gameState.cards?.Count ?? 0}");
            return gameState;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load game: {e.Message}\nStack trace: {e.StackTrace}");
            return null;
        }
    }

    public static bool HasSaveFile()
    {
        string filePath = Path.Combine(Application.persistentDataPath, SAVE_FILE);
        bool exists = File.Exists(filePath);
        Debug.Log($"Save file exists: {exists} at {filePath}");
        return exists;
    }

    public static void DeleteSave()
    {
        try
        {
            string filePath = Path.Combine(Application.persistentDataPath, SAVE_FILE);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log("Save file deleted successfully");
            }
            else
            {
                Debug.Log("No save file to delete");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to delete save file: {e.Message}");
        }
    }

    public static string GetSaveFilePath()
    {
        return Path.Combine(Application.persistentDataPath, SAVE_FILE);
    }
}