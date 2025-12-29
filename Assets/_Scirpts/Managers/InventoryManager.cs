using System;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Inventory manager: handles picking up a single carried item, depositing it to the cabin,
/// quest progress text, simple feedback popup, and saving/loading via ISaveable.
/// Plays SFX by triggering EventManager.ON_PLAY_SFX so a centralized Audio system can route it.
/// </summary>
public class InventoryManager : MonoBehaviour, ISaveable
{
    [Header("Quest Settings")]
    [Tooltip("How many items required in total (example: 5)")]
    [SerializeField] private int totalItemsNeeded = 5;

    [Header("Runtime (debug)")]
    [Tooltip("Shows what we are carrying in Inspector")]
    public ItemDataSO currentItem;

    [Header("UI")]
    [SerializeField] private TMP_Text questText;      // assign in inspector (TextMeshPro)
    [SerializeField] private TMP_Text feedbackText;   // optional small popup text

    [Header("Audio")]
    [Tooltip("Optional: sound played when an item is deposited to the cabin.")]
    [SerializeField] private AudioClip depositSound;

    [Header("Events")]
    [Tooltip("Called once when the quest is completed (useful for hooking VFX/SFX in inspector).")]
    public UnityEvent onQuestCompleted;

    // Internal state
    private int deliveredCount = 0;
    private bool questCompleted = false;

    private void Start()
    {
        UpdateQuestText();
    }

    #region Public API

    /// <summary>
    /// Public read-only accessors so other systems can query state.
    /// </summary>
    public int DeliveredCount => deliveredCount;
    public int TotalItemsNeeded => totalItemsNeeded;
    public bool IsQuestCompleted => questCompleted;

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

        // When an item is collected, show "Get the item to the cabin"
        SetQuestText_GetToCabin();

        // Play item collect SFX via EventManager (AudioManager should handle actual playback)
        if (item.collectSound != null)
        {
            EventManager.TriggerEvent(EventManager.ON_PLAY_SFX, item.collectSound);
        }

        // Notify other systems that an item was collected (payload: ItemDataSO)
        EventManager.TriggerEvent(EventManager.ON_ITEM_COLLECTED, item);

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

        // If already completed, still clear carried item and give feedback but don't change counters
        if (questCompleted)
        {
            Debug.Log("[InventoryManager] Deposited after completion. Clearing carried item.");
            currentItem = null;

            ShowFeedback("Delivered!");
            CancelInvoke(nameof(ShowMemoryRestoredMessage));
            Invoke(nameof(ShowMemoryRestoredMessage), 1.6f);

            return true;
        }


        Debug.Log($"[InventoryManager] Deposited {currentItem.itemName}");

        // Clear the carried item
        currentItem = null;

        // Increment delivered counter (but clamp to totalItemsNeeded)
        deliveredCount = Mathf.Min(totalItemsNeeded, deliveredCount + 1);

        // Update quest UI
        UpdateQuestText();

        // Play deposit SFX via EventManager
        if (depositSound != null)
        {
            EventManager.TriggerEvent(EventManager.ON_PLAY_SFX, depositSound);
        }

        // Optional: check completion
        if (deliveredCount >= totalItemsNeeded && !questCompleted)
        {
            questCompleted = true;
            OnQuestCompleted();
        }
        else
        {
            ShowFeedback("Delivered!");

            // After feedback clears, show narrative message
            CancelInvoke(nameof(ShowMemoryRestoredMessage));
            Invoke(nameof(ShowMemoryRestoredMessage), 1.6f);
        }


        return true;
    }

    #endregion

    #region UI Helpers

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
        EventManager.TriggerEvent(EventManager.ON_GAME_OVER, deliveredCount, totalItemsNeeded);

        // Invoke inspector hook for VFX/SFX
        onQuestCompleted?.Invoke();
    }

    private void ShowFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            // clear after short time
            CancelInvoke(nameof(ClearFeedback));
            Invoke(nameof(ClearFeedback), 1.5f);
        }
    }

    private void ClearFeedback()
    {
        if (feedbackText != null) feedbackText.text = "";
    }
    
    private void ShowMemoryRestoredMessage()
    {
        ShowFeedback("You have restored a part of your memories");
    }

    #endregion

    #region Saving / Loading (ISaveable)

    public void SaveData(ref GameData data)
    {
        data.itemsDelivered = this.deliveredCount;

        if (this.currentItem != null)
        {
            data.currentHeldItemID = this.currentItem.itemID;
        }
        else
        {
            data.currentHeldItemID = "";
        }
    }

    public void LoadData(GameData data)
    {
        this.deliveredCount = data.itemsDelivered;
        UpdateQuestText(); // Refresh UI

        // If loading shows we've already completed, set flag so completion doesn't re-fire later
        if (this.deliveredCount >= totalItemsNeeded)
            questCompleted = true;
        else
            questCompleted = false;

        // Restore held item (if any)
        if (!string.IsNullOrEmpty(data.currentHeldItemID) && PersistenceManager.Instance != null)
        {
            ItemDataSO item = PersistenceManager.Instance.GetItemByID(data.currentHeldItemID);
            if (item != null)
            {
                this.currentItem = item;
                Debug.Log($"[InventoryManager] Restored held item: {item.itemName}");
            }
            else
            {
                this.currentItem = null;
            }
        }
        else
        {
            this.currentItem = null;
        }
    }

    #endregion
}
