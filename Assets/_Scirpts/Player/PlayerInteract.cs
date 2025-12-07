using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;
    [SerializeField] private TMP_Text promptText;
    
    [Header("Detection Settings")]
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private float interactAngle = 30f;
    [SerializeField] private LayerMask interactLayer; // Assign "Collectible" AND "Interactables" here

    [Header("Layer IDs (For Highlighting)")]
    // The name of the layer items sit on by default
    [SerializeField] private string defaultLayerName = "Collectible"; 
    // The name of the layer used for the outline/highlight effect
    [SerializeField] private string highlightLayerName = "Interactables"; 

    private InventoryManager inventoryManager;
    private GameObject currentTarget;
    private GameObject previousTarget;

    private int defaultLayerIndex;
    private int highlightLayerIndex;

    private void Awake()
    {
        inventoryManager = FindObjectOfType<InventoryManager>();
        
        // Fallback if camera isn't assigned
        if (cam == null) cam = Camera.main;

        // Initialize Layer IDs
        defaultLayerIndex = LayerMask.NameToLayer(defaultLayerName);
        highlightLayerIndex = LayerMask.NameToLayer(highlightLayerName);

        // Hide UI on start
        if (promptText != null) promptText.gameObject.SetActive(false);
    }

    private void Update()
    {
        DetectTarget();
        UpdateUI();
        HandleInput();
    }

    // ---------------------------------------------------------
    // STEP 1: Find the Best Target (Logic from Script A)
    // ---------------------------------------------------------
    private void DetectTarget()
    {
        // Reset the previous target's layer (remove highlight)
        if (previousTarget != currentTarget && previousTarget != null)
        {
            previousTarget.layer = defaultLayerIndex;
        }

        previousTarget = currentTarget;
        currentTarget = null; 

        // Find all colliders in range
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, interactLayer);
        
        float closestDist = Mathf.Infinity;
        GameObject bestCandidate = null;

        foreach (Collider hit in hits)
        {
            // Calculate direction to target
            Vector3 dir = (hit.transform.position - cam.transform.position).normalized;
            
            // Check if it is within the Field of View angle
            float angle = Vector3.Angle(cam.transform.forward, dir);

            if (angle < interactAngle)
            {
                float dist = Vector3.Distance(cam.transform.position, hit.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    bestCandidate = hit.gameObject;
                }
            }
        }

        // Set the new current target and apply highlight
        if (bestCandidate != null)
        {
            currentTarget = bestCandidate;
            currentTarget.layer = highlightLayerIndex;
        }
    }

    // ---------------------------------------------------------
    // STEP 2: Update the Text Prompt (Logic from Script B)
    // ---------------------------------------------------------
    private void UpdateUI()
    {
        if (currentTarget == null)
        {
            HidePrompt();
            return;
        }

        // Logic for Collectibles
        Collectible collectible = currentTarget.GetComponent<Collectible>();
        if (collectible != null)
        {
            if (inventoryManager.currentItem == null)
                ShowPrompt("Press E to pick up");
            else
                ShowPrompt("Hands full");
            return;
        }

        // Logic for Cabin
        CabinController cabin = currentTarget.GetComponent<CabinController>();
        if (cabin != null)
        {
            if (inventoryManager.currentItem != null)
                ShowPrompt("Press E to deposit");
            else
                ShowPrompt("Nothing to deposit");
            return;
        }

        // Default fallback if it's interactable but neither of above
        ShowPrompt("Press E to Interact");
    }

    // ---------------------------------------------------------
    // STEP 3: Handle Interaction (Logic from Script B)
    // ---------------------------------------------------------
    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.E) && currentTarget != null)
        {
            Collectible collectible = currentTarget.GetComponent<Collectible>();
            if (collectible != null)
            {
                collectible.Interact();
                return;
            }

            CabinController cabin = currentTarget.GetComponent<CabinController>();
            if (cabin != null)
            {
                cabin.Interact();
                return;
            }
        }
    }

    // ---------------------------------------------------------
    // UI Helpers
    // ---------------------------------------------------------
    private void ShowPrompt(string message)
    {
        if (promptText == null) return;
        promptText.text = message;
        promptText.gameObject.SetActive(true);
    }

    private void HidePrompt()
    {
        if (promptText == null) return;
        promptText.gameObject.SetActive(false);
    }
}