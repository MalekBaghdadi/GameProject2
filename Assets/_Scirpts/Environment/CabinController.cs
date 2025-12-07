using UnityEngine;

/// <summary>
/// Attached to the Cabin drop-off point. Handles the item deposition and visual progression of the cabin.
/// Requires a trigger collider for detection and a Rigidbody (if attached to a static object).
/// </summary>
[RequireComponent(typeof(Collider))]
public class CabinController : MonoBehaviour
{
    [Header("Construction Settings")]
    [Tooltip("Drag the parent objects for each build level here, in the order they should appear.")]
    [SerializeField] private GameObject[] constructionStages;

    [Header("Audio")]
    [Tooltip("Sound played when a new cabin stage is activated.")]
    [SerializeField] private AudioClip cabinUpgradeSound;

    // Internal state tracking
    private int currentStageIndex = 0;
    private bool isPlayerInRange = false;

    private const string PLAYER_TAG = "Player"; // Tag the player GameObject must have
    private InventoryManager inventoryManager;

    private void Start()
    {
        // Get manager reference once on start
        inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager == null)
        {
            Debug.LogError("CabinController failed to find InventoryManager in the scene.");
        }

        // Recommended: Ensure all construction stages are hidden when the game starts.
        foreach (GameObject stage in constructionStages)
        {
            if (stage != null)
            {
                stage.SetActive(false);
            }
        }
    }

    private void Update()
    {
        // Debug quick-check
        {
            if (Input.GetKeyDown(KeyCode.E))
                Debug.Log("E Pressed anywhere");
        }

        // Only check for input if the player is actually in the trigger zone
        if (isPlayerInRange)
        {
            // Check for the 'E' key press
            if (Input.GetKeyDown(KeyCode.E))
            {
                // Call the interaction logic
                Interact();
                Debug.Log("Interacting with cabin!!!!!");
            }
        }
    }

    // Check when a trigger enters the area
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger Entered");
        if (other.CompareTag(PLAYER_TAG))
        {
            isPlayerInRange = true;
            Debug.Log("Player entered Cabin drop-off zone.");
            // TODO: Trigger UI prompt to "Press E to Deposit"
        }
    }

    // Check when a trigger leaves the area
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(PLAYER_TAG))
        {
            isPlayerInRange = false;
            Debug.Log("Player left Cabin drop-off zone.");
            // TODO: Hide the UI prompt
        }
    }

    // The core interaction logic, now called by Update() when 'E' is pressed
    public void Interact()
    {
        if (inventoryManager == null)
        {
            Debug.LogError("InventoryManager is missing.");
            return;
        }

        // 1. Check if the cabin is already finished.
        if (currentStageIndex >= constructionStages.Length)
        {
            Debug.Log("The cabin is fully constructed! No more items needed.");
            return;
        }

        // 2. The crucial check: does the player have an item?
        if (inventoryManager.currentItem == null)
        {
            // Player is pressing E but holding nothing
            Debug.Log("Nothing to deposit yet (Press E failed).");
            // NOTE: The InventoryManager will also show a feedback message.
            inventoryManager.DepositItem(); // Call this to trigger the 'carrying nothing' feedback
            return;
        }

        // 3. Attempt to deposit the item (This also handles inventory clearance and delivered count)
        if (inventoryManager.DepositItem())
        {
            // 4. Successful deposit: Build the next stage
            ActivateNextStage();
        }
        // If DepositItem returned false, it means the inventory was empty, and the feedback was already handled.
    }

    private void ActivateNextStage()
    {
        // Double check bounds to prevent errors
        Debug.Log("Activating next stage.");
        if (currentStageIndex < constructionStages.Length)
        {
            GameObject stageToBuild = constructionStages[currentStageIndex];

            if (stageToBuild != null)
            {
                stageToBuild.SetActive(true);
                Debug.Log($"Item deposited! Constructed Cabin Level {currentStageIndex + 1}.");
            }

            // --- AUDIO: Play cabin upgrade sound (via EventManager so centralized AudioManager handles SFX) ---
            if (cabinUpgradeSound != null)
            {
                EventManager.TriggerEvent(EventManager.ON_PLAY_SFX, cabinUpgradeSound);
            }

            // Increment the index so the next deposit activates the next object
            currentStageIndex++;

            if (currentStageIndex >= constructionStages.Length)
            {
                Debug.Log("Construction Complete!");
                // Optional: Trigger a 'Win' event or UI here
            }
        }
    }
}
