using UnityEngine;

/// <summary>
/// Defines the data for a pickup item (Name, Sound, ID, etc.)
/// Right-click in Project window -> Inventory -> Item Data to create one.
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
public class ItemDataSO : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;

    [Header("Unique ID (auto-generated)")]
    public string itemID; 
    public string ItemID => itemID;  // public read-only property

    [Header("Audio")]
    public AudioClip collectSound;

    // Generate a GUID for this item (Editor only)
    [ContextMenu("Generate New Item ID")]
    private void GenerateID()
    {
        itemID = System.Guid.NewGuid().ToString();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}