using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;
    [SerializeField] private TMP_Text promptText;

    [Header("Detection")]
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private float interactAngle = 30f;
    [SerializeField] private LayerMask interactLayer;

    private GameObject currentTarget;

    private void Awake()
    {
        if (cam == null)
            cam = Camera.main;

        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    private void Update()
    {
        DetectTarget();
        UpdateUI();
        HandleInput();
    }

    // ================= DETECTION =================

    private void DetectTarget()
    {
        currentTarget = null;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            interactRange,
            interactLayer
        );

        float bestScore = float.MaxValue;

        foreach (Collider hit in hits)
        {
            Vector3 dir = (hit.transform.position - cam.transform.position).normalized;
            float angle = Vector3.Angle(cam.transform.forward, dir);

            if (angle > interactAngle)
                continue;

            float dist = Vector3.Distance(cam.transform.position, hit.transform.position);

            if (dist < bestScore)
            {
                bestScore = dist;
                currentTarget = hit.gameObject;
            }
        }
    }

    // ================= UI =================

    private void UpdateUI()
    {
        if (currentTarget == null)
        {
            HidePrompt();
            return;
        }

        IInteractable interactable = currentTarget.GetComponent<IInteractable>();
        if (interactable == null)
        {
            HidePrompt();
            return;
        }

        string prompt = interactable.GetInteractPrompt(this);

        if (string.IsNullOrEmpty(prompt))
        {
            HidePrompt();
            return;
        }

        ShowPrompt(prompt);
    }

    // ================= INPUT =================

    private void HandleInput()
    {
        if (!Input.GetKeyDown(KeyCode.E))
            return;

        if (currentTarget == null)
            return;

        IInteractable interactable = currentTarget.GetComponent<IInteractable>();
        if (interactable == null)
            return;

        interactable.Interact(this);
    }

    // ================= UI HELPERS =================

    private void ShowPrompt(string msg)
    {
        if (promptText == null) return;

        promptText.text = msg;
        promptText.gameObject.SetActive(true);
    }

    private void HidePrompt()
    {
        if (promptText == null) return;

        promptText.gameObject.SetActive(false);
    }
}
