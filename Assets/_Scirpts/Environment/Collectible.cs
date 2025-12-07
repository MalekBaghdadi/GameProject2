using UnityEngine;

public class Collectible : MonoBehaviour, ISaveable
{
    [SerializeField] private string uniqueID;
    [SerializeField] private ItemDataSO itemData;
    [SerializeField] private AudioClip pickupSFX;

    
    // Helper to generate ID in Editor
    [ContextMenu("Generate ID")]
    private void GenerateGuid()
    {
        uniqueID = System.Guid.NewGuid().ToString();
    }

    public void SaveData(ref GameData data)
    {
        // If this object is disabled (collected), add ID to list
        if (!gameObject.activeSelf) 
        {
            if (!data.collectedItemIDs.Contains(uniqueID))
            {
                data.collectedItemIDs.Add(uniqueID);
            }
        }
    }

    public void LoadData(GameData data)
    {
        // Check if my ID is in the "already collected" list
        if (data.collectedItemIDs.Contains(uniqueID))
        {
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
        }
    }

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
        if (collectionSuccessful)
        {
            Debug.Log($"[Collectible] Collected: {itemData.itemName}");

            // PLAY PICKUP SOUND
            if (pickupSFX != null)
            {
                EventManager.TriggerEvent(EventManager.ON_PLAY_SFX, new object[] { pickupSFX, 1f });
            }

            // Disable object
            gameObject.SetActive(false);
        }
    }
    
    
}