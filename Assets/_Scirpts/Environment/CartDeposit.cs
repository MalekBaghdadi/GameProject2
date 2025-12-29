using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CartDeposit : MonoBehaviour, IInteractable
{
    private InventoryManager inventoryManager;

    private void Awake()
    {
        inventoryManager = FindObjectOfType<InventoryManager>();

        if (inventoryManager == null)
        {
            Debug.LogError("[CartDeposit] No InventoryManager found in scene.");
        }
    }

    // ================= INTERACTION =================

    public void Interact(PlayerInteraction player)
    {
        if (inventoryManager == null)
            return;

        inventoryManager.DepositItem();
    }

    // ================= PROMPT TEXT =================

    public string GetInteractPrompt(PlayerInteraction player)
    {
        if (inventoryManager == null)
            return "";

        // Player is carrying something → show deposit prompt
        if (inventoryManager.currentItem != null)
            return "Press E to deposit item";

        // Player is empty-handed
        return "Nothing to deposit";
    }
}