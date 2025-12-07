using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Collectible : MonoBehaviour, ISaveable
{
    [Header("Identity / Data")]
    [SerializeField] private string uniqueID;
    [SerializeField] private ItemDataSO itemData;

    [Header("Sparkle (pooled VFX)")]
    [Tooltip("Optional sparkle prefab instance retrieved from SparklePool")]
    [SerializeField] private GameObject sparkleInstance;

    // Helper to generate ID in Editor
    [ContextMenu("Generate ID")]
    private void GenerateGuid()
    {
        uniqueID = System.Guid.NewGuid().ToString();
    }

    private void Start()
    {
        // Request a sparkle particle from the pool when this collectible spawns
        // (Only if object is active at Start)
        if (gameObject.activeSelf)
        {
            TryAcquireSparkle();
        }
    }

    private void OnEnable()
    {
        // If re-enabled (e.g. after loading), ensure sparkle exists
        TryAcquireSparkle();
    }

    private void OnDisable()
    {
        // Return sparkle to pool when collectible is disabled/collected
        ReturnSparkleToPool();
    }

    private void Update()
    {
        // Keep sparkle positioned above the item if it's active
        if (sparkleInstance != null && sparkleInstance.activeSelf)
        {
            sparkleInstance.transform.position = transform.position + Vector3.up * 0.35f;
        }
    }

    private void TryAcquireSparkle()
    {
        if (sparkleInstance != null) return; // already have one

        if (SparklePool.Instance != null)
        {
            sparkleInstance = SparklePool.Instance.GetSparkle(transform.position + Vector3.up * 0.35f);
        }
        else
        {
            // Not fatal — sparkle is optional
            // Debug.LogWarning("[Collectible] No SparklePool found in the scene.");
        }
    }

    private void ReturnSparkleToPool()
    {
        if (sparkleInstance != null && SparklePool.Instance != null)
        {
            SparklePool.Instance.ReturnSparkle(sparkleInstance);
            sparkleInstance = null;
        }
    }

    // --- Interact / Collection logic ---
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

            // RETURN particle effect to pool (if pooled)
            ReturnSparkleToPool();

            // Disable the collectible object (this will also be saved by SaveData)
            gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("[Collectible] Inventory is full or item rejected.");
        }
    }

    // --- ISaveable interface ---
    public void SaveData(ref GameData data)
    {
        // Ensure we have an ID (useful if created at runtime without setting ID)
        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = System.Guid.NewGuid().ToString();

        // If this object is disabled (collected), add ID to list
        if (!gameObject.activeSelf)
        {
            if (!data.collectedItemIDs.Contains(uniqueID))
            {
                data.collectedItemIDs.Add(uniqueID);
            }
        }
        else
        {
            // If active and previously saved as collected, remove it (optional but keeps save consistent)
            if (data.collectedItemIDs.Contains(uniqueID))
            {
                data.collectedItemIDs.Remove(uniqueID);
            }
        }
    }

    public void LoadData(GameData data)
    {
        // If uniqueID isn't set, we can't match saved state — log a warning.
        if (string.IsNullOrEmpty(uniqueID))
        {
            Debug.LogWarning($"[Collectible] {gameObject.name} has no uniqueID. Generate one via the context menu to enable saving.");
            // leave active by default
            gameObject.SetActive(true);
            return;
        }

        // Check if my ID is in the "already collected" list
        if (data.collectedItemIDs != null && data.collectedItemIDs.Contains(uniqueID))
        {
            // This item was collected previously -> ensure it remains disabled and return sparkle if any
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
            ReturnSparkleToPool();
        }
        else
        {
            // Not collected -> ensure active and re-acquire sparkle
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            TryAcquireSparkle();
        }
    }
}
