using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private LayerMask interactLayer; // Only your interactable objects

    [Header("UI")]
    [SerializeField] private TMP_Text promptText; // Assign ONLY ONE TMP text

    private InventoryManager inventoryManager;

    private void Awake()
    {
        inventoryManager = FindObjectOfType<InventoryManager>();

        // Make sure it starts hidden
        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    private void Update()
    {
        UpdatePrompt();

        if (Input.GetKeyDown(KeyCode.E))
            TryInteract();
    }

    private void UpdatePrompt()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer))
        {
            // Check for collectible
            Collectible collectible = hit.collider.GetComponent<Collectible>();
            if (collectible != null)
            {
                if (inventoryManager.currentItem == null)
                    ShowPrompt("Press E to pick up item");
                else
                    ShowPrompt("Hands full");
                return;
            }

            // Check for cabin
            CabinController cabin = hit.collider.GetComponent<CabinController>();
            if (cabin != null)
            {
                if (inventoryManager.currentItem != null)
                    ShowPrompt("Press E to deposit item");
                else
                    ShowPrompt("Nothing to deposit");
                return;
            }
        }

        HidePrompt();
    }

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

    private void TryInteract()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer))
        {
            Collectible collectible = hit.collider.GetComponent<Collectible>();
            if (collectible != null)
            {
                collectible.Interact();
                return;
            }

            CabinController cabin = hit.collider.GetComponent<CabinController>();
            if (cabin != null)
            {
                cabin.Interact();
                return;
            }
        }
    }
}
