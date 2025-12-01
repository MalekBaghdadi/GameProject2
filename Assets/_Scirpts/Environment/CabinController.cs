using UnityEngine;

/// <summary>
/// Attached to the Cabin drop-off point. Handles the item deposition interaction.
/// </summary>
[RequireComponent(typeof(Collider))] // Requires a trigger collider for detection
public class CabinController : MonoBehaviour
{
    // The collider should be a trigger to enable interaction detection.

    public void Interact()
    {
        
        InventoryManager inventoryManager = FindObjectOfType<InventoryManager>();

        if (inventoryManager == null)
        {
            Debug.LogError("CabinController failed to find InventoryManager.");
            return;
        }

        if (inventoryManager.DepositItem())
        {
            // Successful deposit
            Debug.Log("Item deposited successfully! Ready to collect another.");
            
        }
        else
        {
            // Player is interacting but holding nothing
            Debug.Log("Nothing to deposit yet.");
            // Optional: Trigger UI message saying "You are not carrying any items."
        }
    }
}