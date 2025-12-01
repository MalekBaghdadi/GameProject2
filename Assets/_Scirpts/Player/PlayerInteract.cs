using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactRange = 3.0f;
    [SerializeField] private LayerMask interactableLayer;
    
    [Header("References")]
    [SerializeField] private Camera playerCamera;

    private void Awake()
    {
        // Auto-assign camera if missing
        if (playerCamera == null) playerCamera = Camera.main;
    }

    private void Update()
    {
        CheckForInteraction();
    }

    private void CheckForInteraction()
    {
        // Create a Ray from the center of the screen
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        // Cast Ray
        if (Physics.Raycast(ray, out hit, interactRange, interactableLayer))
        {
            // Debugging: Draw a line to what we are hitting
            Debug.DrawLine(ray.origin, hit.point, Color.green);

            if (Input.GetKeyDown(KeyCode.E))
            {
                // Debugging: Log what we hit to the console
                Debug.Log($"Hit object: {hit.collider.gameObject.name} on Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");

                // Check for Collectible
                Collectible collectible = hit.collider.GetComponent<Collectible>();
                if (collectible != null)
                {
                    collectible.Interact();
                    return;
                }

                // Check for Cabin
                CabinController cabin = hit.collider.GetComponent<CabinController>();
                if (cabin != null)
                {
                    cabin.Interact();
                    return;
                }
            }
        }
        else
        {
            // Debugging: Draw a red line indicating no hit within range on that layer
            Debug.DrawRay(ray.origin, ray.direction * interactRange, Color.red);
        }
    }
}