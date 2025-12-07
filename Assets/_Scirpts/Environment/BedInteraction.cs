using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class BedInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform bedCameraPoint; // Empty positioned above bed
    [SerializeField] private Camera playerCamera;     // Main player camera
    [SerializeField] private MonoBehaviour playerMovement; // Your movement script
    [SerializeField] private CanvasGroup fadeCanvas;  // Fullscreen black canvas
    [SerializeField] private TMP_Text interactionPrompt;  // Press E to Sleep (TMP)

    [Header("Settings")]
    [SerializeField] private float cameraMoveDuration = 1.2f;
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private string nextSceneName = "Level 1 Final";

    private bool playerInRange = false;
    private bool isSleeping = false;

    void Update()
    {
        // Show/hide interaction prompt
        if (interactionPrompt != null)
            interactionPrompt.gameObject.SetActive(playerInRange && !isSleeping);

        // Detect input
        if (playerInRange && !isSleeping && Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(StartSleepingSequence());
        }
    }

    private IEnumerator StartSleepingSequence()
    {
        isSleeping = true;

        // Disable player movement
        if (playerMovement != null)
            playerMovement.enabled = false;

        // Smoothly move the camera to the bed
        yield return StartCoroutine(MoveCameraToBed());

        // Fade to black
        if (fadeCanvas != null)
            yield return StartCoroutine(FadeScreen(1f));

        // Load the next scene
        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator MoveCameraToBed()
    {
        float t = 0f;
        Vector3 startPos = playerCamera.transform.position;
        Quaternion startRot = playerCamera.transform.rotation;

        while (t < cameraMoveDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / cameraMoveDuration);

            playerCamera.transform.position = Vector3.Lerp(startPos, bedCameraPoint.position, p);
            playerCamera.transform.rotation = Quaternion.Slerp(startRot, bedCameraPoint.rotation, p);

            yield return null;
        }

        // Ensure exact final position/rotation
        playerCamera.transform.position = bedCameraPoint.position;
        playerCamera.transform.rotation = bedCameraPoint.rotation;
    }

    private IEnumerator FadeScreen(float targetAlpha)
    {
        float time = 0f;
        float startAlpha = fadeCanvas.alpha;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeCanvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        fadeCanvas.alpha = targetAlpha;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }
}
