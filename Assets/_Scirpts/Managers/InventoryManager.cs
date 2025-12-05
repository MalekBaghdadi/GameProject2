using UnityEngine;
using TMPro; // comment out if not using TMP

public class InventoryManager : MonoBehaviour
{
    [Header("Quest Settings")]
    [Tooltip("How many items required in total (example: 5)")]
    [SerializeField] private int totalItemsNeeded = 5;

    [Header("Runtime (debug)")]
    public ItemDataSO currentItem; // shows what we are carrying in Inspector

    [Header("UI")]
    [SerializeField] private TMP_Text questText; // assign in inspector (TextMeshPro)

    // Optional: temporary message display
    [SerializeField] private TMP_Text feedbackText; // optional small popup text

    private int deliveredCount = 0;

    private void Start()
    {
        UpdateQuestText();
    }

    #region Public API (keeps your existing calls working)

    /// <summary>
    /// Try to pick up an item. Returns true if picked up.
    /// </summary>
    public bool TryCollectItem(ItemDataSO item)
    {
        if (item == null)
        {
            Debug.LogWarning("[InventoryManager] TryCollectItem called with null item.");
            return false;
        }

        // If already carrying something, reject
        if (currentItem != null)
        {
            ShowFeedback("Your hands are full!");
            Debug.Log("[InventoryManager] Cannot pick up. Hands are full!");
            return false;
        }

        // Pick the item up
        currentItem = item;
        Debug.Log($"[InventoryManager] Picked up {item.itemName}");

        // When an item is collected, change quest text to "Get the item to the cabin"
        SetQuestText_GetToCabin();

        // Optional: play sound from itemData if present
        if (item.collectSound != null)
            AudioSource.PlayClipAtPoint(item.collectSound, Camera.main.transform.position);

        return true;
    }

    /// <summary>
    /// Deposit/deliver the carried item to the cabin. Returns true if deposit succeeded.
    /// </summary>
    public bool DepositItem()
    {
        if (currentItem == null)
        {
            ShowFeedback("You are carrying nothing.");
            Debug.Log("[InventoryManager] Deposit failed: no current item.");
            return false;
        }

        Debug.Log($"[InventoryManager] Deposited {currentItem.itemName}");
        // Clear the carried item
        currentItem = null;

        // Increment delivered counter (but clamp to totalItemsNeeded)
        deliveredCount = Mathf.Min(totalItemsNeeded, deliveredCount + 1);

        // Update quest text to next target (unless done)
        UpdateQuestText();

        // Optional: check completion
        if (deliveredCount >= totalItemsNeeded)
        {
            OnQuestCompleted();
        }
        else
        {
            ShowFeedback($"Delivered!");
        }

        return true;
    }

    #endregion

    #region UI helpers

    private void UpdateQuestText()
    {
        if (questText == null) return;

        // If completed
        if (deliveredCount >= totalItemsNeeded)
        {
            questText.text = $"Quest Complete ({deliveredCount}/{totalItemsNeeded})";
            return;
        }

        // Show "Find the Item (X/total)" where X is deliveredCount + 1 (next target number)
        int nextIndex = Mathf.Clamp(deliveredCount + 1, 1, totalItemsNeeded);
        questText.text = $"Find the Item ({nextIndex}/{totalItemsNeeded})";
    }

    private void SetQuestText_GetToCabin()
    {
        if (questText != null)
            questText.text = "Get the item to the cabin";
    }

    private void OnQuestCompleted()
    {
        Debug.Log("[InventoryManager] All items delivered. Quest complete!");
        if (questText != null)
            questText.text = "All items delivered!";

        // Trigger the global game-over / level-complete event via EventManager
        // Optionally pass payload: deliveredCount and totalItemsNeeded
        EventManager.TriggerEvent(EventManager.ON_GAME_OVER, deliveredCount, totalItemsNeeded);
        
    }


    private void ShowFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            // optionally start a coroutine to clear it after a second — keep lightweight here
            CancelInvoke(nameof(ClearFeedback));
            Invoke(nameof(ClearFeedback), 1.5f);
        }
    }

    private void ClearFeedback()
    {
        if (feedbackText != null) feedbackText.text = "";
    }

    #endregion
}
