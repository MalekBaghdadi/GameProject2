using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;

    [Header("Interaction Settings")]
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private float interactAngle = 30f;
    [SerializeField] private LayerMask collectibleLayer;

    private GameObject currentTarget;
    private GameObject previousTarget;

    private int collectibleLayerIndex;
    private int interactablesLayerIndex;

    private void Start()
    {
        collectibleLayerIndex = LayerMask.NameToLayer("Collectible");
        interactablesLayerIndex = LayerMask.NameToLayer("Interactables");
    }

    private void Update()
    {
        DetectItem();
        HandleInteraction();
    }

    private void DetectItem()
    {
        // STEP 1: Clear highlight from last frame if needed
        if (previousTarget != currentTarget && previousTarget != null)
        {
            previousTarget.layer = collectibleLayerIndex;
        }

        previousTarget = currentTarget;
        currentTarget = null;  // Reset for this frame

        // STEP 2: Detect items in range
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, collectibleLayer);

        float closestDist = Mathf.Infinity;
        GameObject bestCandidate = null;

        foreach (Collider hit in hits)
        {
            Vector3 dir = (hit.transform.position - cam.transform.position).normalized;
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

        // STEP 3: Highlight the best candidate
        if (bestCandidate != null)
        {
            currentTarget = bestCandidate;
            currentTarget.layer = interactablesLayerIndex;
        }
    }

    private void HandleInteraction()
    {
        if (currentTarget == null)
            return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            Collectible c = currentTarget.GetComponent<Collectible>();
            if (c != null)
            {
                c.Interact();
            }
        }
    }
}
