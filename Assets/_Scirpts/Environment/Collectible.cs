using UnityEngine;

public class Collectible : MonoBehaviour
{
    [SerializeField] private ItemDataSO itemData;

    // Reference to the pooled sparkle
    private GameObject sparkleInstance;

    private void Start()
    {
        // Request a sparkle particle from the pool when this collectible spawns
        if (SparklePool.Instance != null)
        {
            sparkleInstance = SparklePool.Instance.GetSparkle(
                transform.position + Vector3.up * 0.35f   // offset above the item
            );
        }
        else
        {
            Debug.LogWarning("[Collectible] No SparklePool found in the scene.");
        }
    }

    private void Update()
    {
        // Keep sparkle positioned above the item if it's active
        if (sparkleInstance != null && sparkleInstance.activeSelf)
        {
            sparkleInstance.transform.position =
                transform.position + Vector3.up * 0.35f;
        }
    }

    public void Interact()
    {
        if (itemData == null)
        {
            Debug.LogError($"[Collectible] {gameObject.name} is missing ItemDataSO assignment!");
            return;
        }

        InventoryManager inventoryManager = FindObjectOfType<InventoryManager>();

        if (inventoryManager == null)
        {
            Debug.LogError("[Collectible] CRITICAL ERROR: No 'InventoryManager' found in the scene. The item cannot be collected without it.");
            return;
        }

        bool collectionSuccessful = inventoryManager.TryCollectItem(itemData);

        if (collectionSuccessful)
        {
            Debug.Log($"[Collectible] Collected: {itemData.itemName}");

            // RETURN particle effect to pool
            if (sparkleInstance != null)
                SparklePool.Instance.ReturnSparkle(sparkleInstance);

            // Disable the collectible object
            gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("[Collectible] Inventory is full or item rejected.");
        }
    }
}
