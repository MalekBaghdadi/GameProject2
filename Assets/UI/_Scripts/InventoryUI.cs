using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("Slot setup")]
    public RectTransform slotsContainer;    // assign SlotsContainer (contains exactly 5 child slot GameObjects)
    public Sprite emptySprite;              // optional placeholder for empty icon (can be null)
    public Color emptyColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color filledColor = Color.white;

    // Internal representation of UI slot views
    class SlotViews
    {
        public GameObject root;
        public Image background;
        public Image icon;
    }

    List<SlotViews> slots = new List<SlotViews>();
    int selectedIndex = 0;

    // Simple data model: -1 = empty, otherwise item id
    int[] itemIds = new int[5];    // -1 = empty
    int[] itemCounts = new int[5]; // counts kept internally (no count text)

    void Awake()
    {
        // initialize arrays
        for (int i = 0; i < itemIds.Length; i++) { itemIds[i] = -1; itemCounts[i] = 0; }

        // capture child slot views (assumes exactly 5 children)
        slots.Clear();
        for (int i = 0; i < slotsContainer.childCount && i < 5; i++)
        {
            var r = slotsContainer.GetChild(i).gameObject;
            var bg = r.GetComponent<Image>();
            var icon = r.transform.Find("ItemIcon")?.GetComponent<Image>();
            slots.Add(new SlotViews { root = r, background = bg, icon = icon });
        }

        // initial UI state
        RefreshAllSlots();
        SelectSlot(0);
    }

    void RefreshAllSlots()
    {
        for (int i = 0; i < slots.Count; i++) RefreshSlot(i);
    }

    void RefreshSlot(int i)
    {
        var s = slots[i];
        if (itemIds[i] == -1)
        {
            // empty
            if (s.icon != null) { s.icon.sprite = emptySprite; s.icon.enabled = (emptySprite != null); }
            if (s.background != null) s.background.color = emptyColor;
        }
        else
        {
            // filled
            if (s.icon != null) { s.icon.enabled = true; /* sprite set via ApplyIconToSlot */ }
            if (s.background != null) s.background.color = filledColor;
        }
    }

    void ApplyIconToSlot(int index, Sprite icon)
    {
        var s = slots[index];
        if (s.icon != null)
        {
            s.icon.sprite = icon;
            s.icon.enabled = (icon != null);
        }
    }

    /// <summary>
    /// Add an item to inventory.
    /// itemId: integer identifier
    /// icon: sprite to show
    /// count: number to add (default 1)
    /// Returns true if added, false if full.
    /// </summary>
    public bool AddItem(int itemId, Sprite icon, int count = 1)
    {
        // try to stack first (same itemId stacks)
        for (int i = 0; i < itemIds.Length; i++)
        {
            if (itemIds[i] == itemId)
            {
                itemCounts[i] += count;
                ApplyIconToSlot(i, icon);
                RefreshSlot(i);
                return true;
            }
        }

        // find empty slot
        for (int i = 0; i < itemIds.Length; i++)
        {
            if (itemIds[i] == -1)
            {
                itemIds[i] = itemId;
                itemCounts[i] = count;
                ApplyIconToSlot(i, icon);
                RefreshSlot(i);
                return true;
            }
        }

        return false; // no space
    }

    public void RemoveFromSlot(int index, int count = 1)
    {
        if (index < 0 || index >= itemIds.Length) return;
        if (itemIds[index] == -1) return;
        itemCounts[index] -= count;
        if (itemCounts[index] <= 0)
        {
            itemIds[index] = -1;
            itemCounts[index] = 0;
            if (slots[index].icon != null) { slots[index].icon.sprite = emptySprite; slots[index].icon.enabled = (emptySprite != null); }
        }
        RefreshSlot(index);
    }

    /// <summary>
    /// Select a slot index as the held item. 
    /// </summary>
    public void SelectSlot(int index)
    {
        if (index < 0 || index >= itemIds.Length) return;
        selectedIndex = index;
        // No visual change per your request (kept internal only).
    }

    // Inspectors / helpers:
    public int GetSelectedIndex() => selectedIndex;
    public int GetItemIdInSlot(int index) => (index >= 0 && index < itemIds.Length) ? itemIds[index] : -1;
    public int GetItemCountInSlot(int index) => (index >= 0 && index < itemCounts.Length) ? itemCounts[index] : 0;
}
