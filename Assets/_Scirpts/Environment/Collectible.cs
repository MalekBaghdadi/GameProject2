using System;
using UnityEngine;
using System.Collections;

// Requires a collider for interaction detection
[RequireComponent(typeof(Collider))]
public class Collectible : MonoBehaviour, ISaveable
{
    [Header("Identity / Data")]
    [SerializeField] 
    [Tooltip("Unique ID is crucial for saving/loading. Use the 'Generate ID' context menu.")]
    private string uniqueID;
    [SerializeField] private ItemDataSO itemData;

    [Header("Visuals & Audio")]
    [SerializeField] private AudioClip pickupSFX;
    [Tooltip("Optional sparkle prefab instance retrieved from SparklePool")]
    [SerializeField] private GameObject sparkleInstance;

    // --- Editor Helpers ---
    // Helper to generate ID in Editor
    [ContextMenu("Generate ID")]
    private void GenerateGuid()
    {
        uniqueID = System.Guid.NewGuid().ToString();
    }

    // --- Lifecycle and VFX Management (From Script B) ---
    private void Start()
    {
        // Request a sparkle particle from the pool when this collectible spawns
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
            // Adjust position offset as needed
            sparkleInstance.transform.position = transform.position + Vector3.up * 0.35f; 
        }
    }
    
    // --- Sparkle Pool Logic ---
    private void TryAcquireSparkle()
    {
        if (sparkleInstance != null) return; // already have one

        if (SparklePool.Instance != null)
        {
            sparkleInstance = SparklePool.Instance.GetSparkle(transform.position + Vector3.up * 0.35f);
        }
        // No warning if no pool exists, as it's optional visual flair
    }

    private void ReturnSparkleToPool()
    {
        if (sparkleInstance != null && SparklePool.Instance != null)
        {
            SparklePool.Instance.ReturnSparkle(sparkleInstance);
            sparkleInstance = null;
        }
    }

    // --- Interaction Logic (Combined) ---
    public void Interact()
    {
        if (itemData == null)
        {
            Debug.LogError($"[Collectible] {gameObject.name} is missing ItemDataSO assignment!");
            return;
        }

        // Find the Inventory Manager
        InventoryManager inventoryManager = FindObjectOfType<InventoryManager>();

        if (inventoryManager == null)
        {
            Debug.LogError("[Collectible] CRITICAL ERROR: No 'InventoryManager' found in the scene.");
            return;
        }

        // Attempt to collect
        bool collectionSuccessful = inventoryManager.TryCollectItem(itemData);

        if (collectionSuccessful)
        {
            Debug.Log($"[Collectible] Collected: {itemData.itemName}");
            
            // PLAY PICKUP SOUND (From Script A)
            if (pickupSFX != null)
            {
                // NOTE: Assuming EventManager is a static class accessible here
                // Replace with your actual audio trigger method if different
                EventManager.TriggerEvent(EventManager.ON_PLAY_SFX, new object[] { pickupSFX, 1f });
            }

            // VFX Cleanup (From Script B)
            ReturnSparkleToPool();

            // Disable object to make it 'disappear'
            gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("[Collectible] Inventory is full or item rejected.");
        }
    }

    // --- ISaveable interface (Combined and Robust) ---
    public void SaveData(ref GameData data)
    {
        // Ensure ID exists before saving
        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = System.Guid.NewGuid().ToString();

        // If collected (disabled), ensure ID is in the list
        if (!gameObject.activeSelf)
        {
            if (!data.collectedItemIDs.Contains(uniqueID))
            {
                data.collectedItemIDs.Add(uniqueID);
            }
        }
        else
        {
            // Optional: If active, remove it from the collected list (in case it was dropped/respawned)
            if (data.collectedItemIDs.Contains(uniqueID))
            {
                data.collectedItemIDs.Remove(uniqueID);
            }
        }
    }

    public void LoadData(GameData data)
    {
        // ID check (From Script B for robustness)
        if (string.IsNullOrEmpty(uniqueID))
        {
            Debug.LogWarning($"[Collectible] {gameObject.name} has no uniqueID. Skipping load state.");
            gameObject.SetActive(true);
            return;
        }

        // Check if my ID is in the "already collected" list
        if (data.collectedItemIDs != null && data.collectedItemIDs.Contains(uniqueID))
        {
            // This item was collected -> disable it and clean up VFX
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
            ReturnSparkleToPool();
        }
        else
        {
            // Not collected -> ensure active and show VFX
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            TryAcquireSparkle();
        }
    }
}