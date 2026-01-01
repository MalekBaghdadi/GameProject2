using System;
using System.Collections;
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
    [SerializeField] private TMP_Text questText;      
    [SerializeField] private TMP_Text feedbackText;   

    [Header("Quest Text Animation")]
    [SerializeField] private Color questHighlightColor = Color.green;
    [SerializeField] private float highlightDuration = 1.2f;
    [SerializeField] private float morphFadeDuration = 0.35f;

    [Header("Audio")]
    [Tooltip("Optional: sound played when an item is deposited to the cabin.")]
    [SerializeField] private AudioClip depositSound;

    [Header("Events")]
    [Tooltip("Called once when the quest is completed (useful for hooking VFX/SFX in inspector).")]
    public UnityEvent onQuestCompleted;

    // Internal state
    private int deliveredCount = 0;
    private bool questCompleted = false;

    // Quest text animation control
    private Coroutine questTextRoutine;

    private void Start()
    {
        UpdateQuestText();
    }

    #region Public API

    public int DeliveredCount => deliveredCount;
    public int TotalItemsNeeded => totalItemsNeeded;
    public bool IsQuestCompleted => questCompleted;

    public bool TryCollectItem(ItemDataSO item)
    {
        if (item == null)
        {
            Debug.LogWarning("[InventoryManager] TryCollectItem called with null item.");
            return false;
        }

        if (currentItem != null)
        {
            ShowFeedback("Your hands are full!");
            return false;
        }

        // Pick up item
        currentItem = item;
        Debug.Log($"[InventoryManager] Picked up {item.itemName}");

        // QUEST TEXT: green flash + morph to "Get to cabin"
        if (questText != null)
        {
            questTextRoutine = StartCoroutine(
                QuestTextPickupTransition("Get the item to the cabin")
            );
        }

        if (item.collectSound != null)
            EventManager.TriggerEvent(EventManager.ON_PLAY_SFX, item.collectSound);

        EventManager.TriggerEvent(EventManager.ON_ITEM_COLLECTED, item);

        return true;
    }

    public bool DepositItem()
    {
        if (currentItem == null)
        {
            ShowFeedback("You are carrying nothing.");
            return false;
        }

        if (questCompleted)
        {
            currentItem = null;
            ShowFeedback("Delivered!");
            CancelInvoke(nameof(ShowMemoryRestoredMessage));
            Invoke(nameof(ShowMemoryRestoredMessage), 1.6f);
            return true;
        }

        currentItem = null;
        deliveredCount = Mathf.Min(totalItemsNeeded, deliveredCount + 1);

        UpdateQuestText();

        if (depositSound != null)
            EventManager.TriggerEvent(EventManager.ON_PLAY_SFX, depositSound);

        if (deliveredCount >= totalItemsNeeded && !questCompleted)
        {
            questCompleted = true;
            OnQuestCompleted();
        }
        else
        {
            ShowFeedback("Delivered!");
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

        if (deliveredCount >= totalItemsNeeded)
        {
            questText.text = $"Quest Complete ({deliveredCount}/{totalItemsNeeded})";
            return;
        }

        int nextIndex = Mathf.Clamp(deliveredCount + 1, 1, totalItemsNeeded);
        questText.text = $"Find the Item ({nextIndex}/{totalItemsNeeded})";
    }

    private IEnumerator QuestTextPickupTransition(string nextText)
    {
        if (questTextRoutine != null)
            StopCoroutine(questTextRoutine);

        questTextRoutine = null;

        Color originalColor = questText.color;
        questText.alpha = 1f;

        // 1. Green flash
        questText.color = questHighlightColor;
        yield return new WaitForSeconds(highlightDuration);

        // 2. Fade out
        float t = 0f;
        while (t < morphFadeDuration)
        {
            t += Time.deltaTime;
            questText.alpha = Mathf.Lerp(1f, 0f, t / morphFadeDuration);
            yield return null;
        }

        // 3. Swap text
        questText.text = nextText;

        // 4. Fade in
        t = 0f;
        while (t < morphFadeDuration)
        {
            t += Time.deltaTime;
            questText.alpha = Mathf.Lerp(0f, 1f, t / morphFadeDuration);
            yield return null;
        }

        // 5. Restore color
        questText.color = originalColor;
        questText.alpha = 1f;
    }

    private void OnQuestCompleted()
    {
        if (questText != null)
            questText.text = "All items delivered!";

        EventManager.TriggerEvent(
            EventManager.ON_GAME_OVER,
            deliveredCount,
            totalItemsNeeded
        );

        onQuestCompleted?.Invoke();
    }

    private void ShowFeedback(string message)
    {
        if (feedbackText == null) return;

        feedbackText.text = message;
        CancelInvoke(nameof(ClearFeedback));
        Invoke(nameof(ClearFeedback), 1.5f);
    }

    private void ClearFeedback()
    {
        if (feedbackText != null)
            feedbackText.text = "";
    }

    private void ShowMemoryRestoredMessage()
    {
        ShowFeedback("You have restored a part of your memories");
    }

    #endregion

    #region Saving / Loading

    public void SaveData(ref GameData data)
    {
        data.itemsDelivered = deliveredCount;
        data.currentHeldItemID = currentItem != null ? currentItem.itemID : "";
    }

    public void LoadData(GameData data)
    {
        deliveredCount = data.itemsDelivered;
        questCompleted = deliveredCount >= totalItemsNeeded;

        UpdateQuestText();

        if (!string.IsNullOrEmpty(data.currentHeldItemID) &&
            PersistenceManager.Instance != null)
        {
            currentItem =
                PersistenceManager.Instance.GetItemByID(data.currentHeldItemID);
        }
        else
        {
            currentItem = null;
        }
    }

    #endregion
}
