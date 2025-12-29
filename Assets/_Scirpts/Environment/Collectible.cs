using System;
using UnityEngine;
using System.Collections;

// Requires a collider for interaction detection
[RequireComponent(typeof(Collider))]
public class Collectible : MonoBehaviour, ISaveable, IInteractable
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

    // ================= EDITOR HELPERS =================

    [ContextMenu("Generate ID")]
    private void GenerateGuid()
    {
        uniqueID = Guid.NewGuid().ToString();
    }

    // ================= LIFECYCLE =================

    private void Start()
    {
        if (gameObject.activeSelf)
            TryAcquireSparkle();
    }

    private void OnEnable()
    {
        TryAcquireSparkle();
    }

    private void OnDisable()
    {
        ReturnSparkleToPool();
    }

    private void Update()
    {
        if (sparkleInstance != null && sparkleInstance.activeSelf)
        {
            sparkleInstance.transform.position = transform.position + Vector3.up * 0.35f;
        }
    }

    // ================= SPARKLE POOL =================

    private void TryAcquireSparkle()
    {
        if (sparkleInstance != null)
            return;

        if (SparklePool.Instance != null)
        {
            sparkleInstance = SparklePool.Instance.GetSparkle(
                transform.position + Vector3.up * 0.35f
            );
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

    // ================= IINTERACTABLE =================

    public string GetInteractPrompt(PlayerInteraction player)
    {
        InventoryManager inventoryManager = FindObjectOfType<InventoryManager>();

        if (inventoryManager == null)
            return string.Empty;

        if (inventoryManager.currentItem == null)
            return "Press E to pick up";

        return "Hands full";
    }

    public void Interact(PlayerInteraction player)
    {
        if (itemData == null)
        {
            Debug.LogError($"[Collectible] {gameObject.name} is missing ItemDataSO!");
            return;
        }

        InventoryManager inventoryManager = FindObjectOfType<InventoryManager>();

        if (inventoryManager == null)
        {
            Debug.LogError("[Collectible] CRITICAL: No InventoryManager found.");
            return;
        }

        // Attempt to collect item
        bool collected = inventoryManager.TryCollectItem(itemData);

        if (!collected)
        {
            Debug.Log("[Collectible] Inventory full or item rejected.");
            return;
        }

        Debug.Log($"[Collectible] Collected: {itemData.itemName}");

        // Play pickup sound
        if (pickupSFX != null)
        {
            EventManager.TriggerEvent(
                EventManager.ON_PLAY_SFX,
                new object[] { pickupSFX, 1f }
            );
        }

        // Cleanup VFX and disable
        ReturnSparkleToPool();
        gameObject.SetActive(false);
    }

    // ================= ISAVEABLE =================

    public void SaveData(ref GameData data)
    {
        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = Guid.NewGuid().ToString();

        if (!gameObject.activeSelf)
        {
            if (!data.collectedItemIDs.Contains(uniqueID))
                data.collectedItemIDs.Add(uniqueID);
        }
        else
        {
            if (data.collectedItemIDs.Contains(uniqueID))
                data.collectedItemIDs.Remove(uniqueID);
        }
    }

    public void LoadData(GameData data)
    {
        if (string.IsNullOrEmpty(uniqueID))
        {
            Debug.LogWarning(
                $"[Collectible] {gameObject.name} has no uniqueID. Skipping load."
            );
            gameObject.SetActive(true);
            return;
        }

        bool wasCollected =
            data.collectedItemIDs != null &&
            data.collectedItemIDs.Contains(uniqueID);

        if (wasCollected)
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);

            ReturnSparkleToPool();
        }
        else
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            TryAcquireSparkle();
        }
    }
}
