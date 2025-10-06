using UnityEngine;

[CreateAssetMenu(fileName = "CardData", menuName = "Card Match/Card Data", order = 0)]
public class CardData : ScriptableObject
{
    public int id;
    public Sprite faceSprite;
    public string displayName;
}
