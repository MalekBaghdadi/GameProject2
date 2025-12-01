using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "GameData/Item Data")]
public class ItemDataSO : ScriptableObject
{
    public string itemName;
    public string itemID; // Unique ID for tracking
    public Sprite icon;
    public AudioClip collectSound;
}