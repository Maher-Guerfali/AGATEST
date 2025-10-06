using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardSet", menuName = "Card Game/Card Set")]
public class CardSet : ScriptableObject
{
    [Header("Card Sprites")]
    public Sprite backSprite; // The back design for all cards
    public List<Sprite> frontSprites = new List<Sprite>(); // Different front designs

    [Header("Card Set Info")]
    public string setName = "Default Card Set";
    [TextArea(3, 5)]
    public string description = "A collection of card sprites for the memory game.";

    /// <summary>
    /// Gets a random front sprite from the collection
    /// </summary>
    public Sprite GetRandomFrontSprite()
    {
        if (frontSprites.Count == 0)
        {
            Debug.LogWarning("No front sprites available in CardSet!");
            return null;
        }

        int randomIndex = Random.Range(0, frontSprites.Count);
        return frontSprites[randomIndex];
    }

    /// <summary>
    /// Gets a front sprite by index, with wrapping if index is out of bounds
    /// </summary>
    public Sprite GetFrontSprite(int index)
    {
        if (frontSprites.Count == 0)
        {
            Debug.LogWarning("No front sprites available in CardSet!");
            return null;
        }

        // Use modulo to wrap around if index is larger than available sprites
        int wrappedIndex = index % frontSprites.Count;
        return frontSprites[wrappedIndex];
    }

    /// <summary>
    /// Validates the card set and returns true if it's properly configured
    /// </summary>
    public bool IsValid()
    {
        bool hasBackSprite = backSprite != null;
        bool hasFrontSprites = frontSprites.Count > 0;
        bool noNullFrontSprites = frontSprites.TrueForAll(sprite => sprite != null);

        return hasBackSprite && hasFrontSprites && noNullFrontSprites;
    }

    /// <summary>
    /// Gets the number of unique front sprites available
    /// </summary>
    public int GetFrontSpriteCount()
    {
        return frontSprites.Count;
    }

    /// <summary>
    /// Removes null sprites from the front sprites list
    /// </summary>
    [ContextMenu("Clean Null Sprites")]
    public void CleanNullSprites()
    {
        frontSprites.RemoveAll(sprite => sprite == null);
        Debug.Log($"CardSet '{setName}' cleaned. Remaining sprites: {frontSprites.Count}");
    }

    /// <summary>
    /// Shuffles the front sprites list
    /// </summary>
    [ContextMenu("Shuffle Front Sprites")]
    public void ShuffleFrontSprites()
    {
        for (int i = 0; i < frontSprites.Count; i++)
        {
            int randomIndex = Random.Range(i, frontSprites.Count);
            (frontSprites[i], frontSprites[randomIndex]) = (frontSprites[randomIndex], frontSprites[i]);
        }
        Debug.Log($"CardSet '{setName}' front sprites shuffled");
    }

    /// <summary>
    /// Validates the card set in the editor and logs any issues
    /// </summary>
    [ContextMenu("Validate Card Set")]
    public void ValidateCardSet()
    {
        List<string> issues = new List<string>();

        if (backSprite == null)
            issues.Add("Back sprite is missing");

        if (frontSprites.Count == 0)
            issues.Add("No front sprites assigned");
        else
        {
            int nullCount = frontSprites.FindAll(sprite => sprite == null).Count;
            if (nullCount > 0)
                issues.Add($"{nullCount} front sprites are null");
        }

        if (string.IsNullOrEmpty(setName))
            issues.Add("Set name is empty");

        if (issues.Count == 0)
        {
            Debug.Log($"CardSet '{setName}' is valid with {frontSprites.Count} front sprites");
        }
        else
        {
            Debug.LogWarning($"CardSet '{setName}' has issues:\n- " + string.Join("\n- ", issues));
        }
    }
}