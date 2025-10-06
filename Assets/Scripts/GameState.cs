

using System.Collections.Generic;

[System.Serializable]
public class CardState
{
    public int faceId;
    public bool isMatched;
    public bool isRevealed;
}

[System.Serializable]
public class GameState
{
    public int rows;
    public int cols;
    public int score;
    public int combo;
    public List<CardState> cards;
}
