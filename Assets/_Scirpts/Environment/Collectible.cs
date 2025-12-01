using UnityEngine;

public class Collectible : MonoBehaviour
{
    [SerializeField] private ItemDataSO itemData;

    public void Interact()
    {
        if (itemData == null)
        {
            Debug.LogError($"[Collectible] {gameObject.name} is missing ItemDataSO assignment!");
            return;
        }

        // Find the Inventory Manager
        InventoryManager inventoryManager = FindObjectOfType<InventoryManager>();

        // ERROR CHECK: Does the manager exist?
        if (inventoryManager == null)
        {
            Debug.LogError("[Collectible] CRITICAL ERROR: No 'InventoryManager' found in the scene. The item cannot be collected without it.");
            return;
        }

        // Attempt to collect
        bool collectionSuccessful = inventoryManager.TryCollectItem(itemData);

        if (collectionSuccessful)
        {
            Debug.Log($"[Collectible] Collected: {itemData.itemName}");
            // Disable object to make it 'disappear'
            gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("[Collectible] Inventory is full or item rejected.");
        }
    }
}