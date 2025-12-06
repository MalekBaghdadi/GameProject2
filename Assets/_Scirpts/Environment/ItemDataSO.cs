using UnityEngine;

/// <summary>
/// Defines the data for a pickup item (Name, Sound, etc.)
/// Right-click in Project window -> Inventory -> Item Data to create one.
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
public class ItemDataSO : ScriptableObject
{
    public string itemName;
    public AudioClip collectSound;
    // You can add more fields here like 'icon' or 'prefab' if needed.
}