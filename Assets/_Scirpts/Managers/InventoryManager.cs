using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("Debug")]
    public ItemDataSO currentItem; // Shows what we are carrying in Inspector

    /// <summary>
    /// Tries to collect an item. Returns true if successful.
    /// </summary>
    public bool TryCollectItem(ItemDataSO item)
    {
        // Simple Logic: Can only carry one item at a time
        if (currentItem == null)
        {
            currentItem = item;
            Debug.Log($"[InventoryManager] Picked up {item.itemName}");
            return true;
        }
        
        Debug.Log("[InventoryManager] Cannot pick up. Hands are full!");
        return false;
    }

    /// <summary>
    /// Removes the item from inventory. Returns true if something was dropped/deposited.
    /// </summary>
    public bool DepositItem()
    {
        if (currentItem != null)
        {
            Debug.Log($"[InventoryManager] Deposited {currentItem.itemName}");
            currentItem = null;
            return true;
        }
        return false;
    }
}