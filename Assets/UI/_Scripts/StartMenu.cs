using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Start menu + camera transition helper.
/// - Assign player (GameObject), the player's camera transform (target), and a top-down start transform.
/// - Prefer to assign the exact player controller / look scripts in the inspector to reliably enable/disable them.
/// </summary>
public class StartMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject contentRoot;      // StartMenu_Content (optional)
    public Button startButton;
    public Button quitButton;
    
    [Header("Game UI")]
    [Tooltip("Assign the HUD root GameObject here to toggle it with the transition.")]
    public GameObject HUDContentRoot;

    [Header("Camera transition")]
    public Transform startCamTransform;    // StartCam_TopDown (scene-root transform)
    public Transform playerCamTransform;   // Player's camera transform (child of player)
    [Tooltip("If true, the script will move Camera.main during transition.")]
    public bool moveMainCamera = true;
    public float transitionDuration = 2.0f;
    public bool disablePlayerInputDuringTransition = true;

    [Header("Player/controls (recommended)")]
    [Tooltip("Assign the Player GameObject here (recommended).")]
    public GameObject player; // drag your Player in inspector

    [Tooltip("Explicit list of MonoBehaviour components on the player to disable while transitioning. Recommended.")]
    public MonoBehaviour[] playerControlComponents;

    // Internal
    Camera mainCamera;
    Transform originalCameraParent;
    Coroutine transitionCoroutine;

    void Awake()
    {
        if (startButton != null) startButton.onClick.AddListener(OnStartPressed);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitPressed);

        if (moveMainCamera)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) Debug.LogWarning("StartMenu: Camera.main not found.");
        }
    }

    void Start()
    {
        // Ensure Start menu is visible and on top
        if (contentRoot != null) contentRoot.SetActive(true);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        // Make sure the pause menu isn't blocking input
        // (Optional - you can remove if already set)
        // var pauseGO = GameObject.Find("Pause");
        // if (pauseGO) pauseGO.SetActive(false);

        // --- ADD THESE TWO LINES: show and unlock the cursor while the Start Menu is active ---
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        // -------------------------------------------------------------------------------

        // If using main camera, snap it to start transform at beginning (unparent, set world pos/rot)
        if (moveMainCamera && mainCamera != null && startCamTransform != null)
        {
            originalCameraParent = mainCamera.transform.parent;
            mainCamera.transform.SetParent(null, true); // unparent so player won't override position
            mainCamera.transform.position = startCamTransform.position;
            mainCamera.transform.rotation = startCamTransform.rotation;
        }

        // Optionally disable player control immediately so player can't move while Start menu is visible
        if (disablePlayerInputDuringTransition && player != null)
        {
            SetPlayerControlsEnabled(false);
        }
        
        if (HUDContentRoot != null) HUDContentRoot.SetActive(false);
    }

    public void OnStartPressed()
    {
        Debug.Log("Start pressed");
        if (transitionCoroutine != null) return;
        transitionCoroutine = StartCoroutine(CameraTransitionCoroutine());
    }

    IEnumerator CameraTransitionCoroutine()
    {
        // 1. Safety Checks
        if (moveMainCamera && mainCamera == null) yield break;
        if (startCamTransform == null || playerCamTransform == null) yield break;

        // --- NEW: Hide UI and Cursor IMMEDIATELY ---
        if (contentRoot != null) contentRoot.SetActive(false);
        else gameObject.SetActive(false); // Fallback if contentRoot isn't assigned

        // Hide cursor immediately for a clean cinematic look
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        // -------------------------------------------

        // 2. FORCE TimeScale to 1
        Time.timeScale = 1f;

        // 3. Disable Player Controls
        if (disablePlayerInputDuringTransition)
            SetPlayerControlsEnabled(false);

        // 4. Detach Camera
        if (moveMainCamera)
        {
            if (mainCamera.transform.parent != null) originalCameraParent = mainCamera.transform.parent;
            mainCamera.transform.SetParent(null, true);
        }

        Transform camT = mainCamera.transform;
        Vector3 fromPos = camT.position;
        Quaternion fromRot = camT.rotation;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, transitionDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / duration);

            // Smooth Ease Out (starts fast, slows down at the end)
            float eased = 1f - Mathf.Pow(1f - alpha, 3f); 

            // Live target update
            Vector3 toPosLive = playerCamTransform.position;
            Quaternion toRotLive = playerCamTransform.rotation;

            camT.position = Vector3.Lerp(fromPos, toPosLive, eased);
            camT.rotation = Quaternion.Slerp(fromRot, toRotLive, eased);

            yield return new WaitForEndOfFrame();
        }

        // 5. Final Snap & Reparent
        camT.position = playerCamTransform.position;
        camT.rotation = playerCamTransform.rotation;

        if (moveMainCamera)
        {
            Transform desiredParent = playerCamTransform.parent != null ? playerCamTransform.parent : player.transform;
            camT.SetParent(desiredParent, true);
        }
        
        if (HUDContentRoot != null) HUDContentRoot.SetActive(true);

        // 6. Final Game State Setup
        GameState.IsGameStarted = true;

        if (disablePlayerInputDuringTransition)
            SetPlayerControlsEnabled(true);

        transitionCoroutine = null;
        gameObject.SetActive(false);
    }



    /// <summary>
    /// Enables/disables the assigned components. If none assigned, tries to auto-detect common controller scripts on the assigned player.
    /// </summary>
    void SetPlayerControlsEnabled(bool enable)
    {
        if (player == null)
        {
            Debug.LogWarning("StartMenu: player GameObject not assigned. Trying to find by tag 'Player'.");
            player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("StartMenu: Could not find Player. Skipping toggle of player controls.");
                return;
            }
        }

        // If explicit components were assigned, use them (recommended)
        if (playerControlComponents != null && playerControlComponents.Length > 0)
        {
            foreach (var comp in playerControlComponents)
            {
                if (comp == null) continue;
                comp.enabled = enable;
            }
            return;
        }

        // Fallback: attempt to auto-detect common controller scripts on the player
        var behaviours = player.GetComponents<MonoBehaviour>();
        if (behaviours == null || behaviours.Length == 0) return;

        foreach (var b in behaviours)
        {
            if (b == null) continue;
            string n = b.GetType().Name;

            // Skip UI / menu scripts
            if (n.Contains("UI") || n.Contains("Menu") || n.Contains("Inventory") || n.Contains("StartMenu")) continue;

            // Try a whitelist of likely input controllers to toggle (customize as needed)
            if (n == "PlayerController" || n == "MouseLook" || n == "PlayerMovement" || n == "FirstPersonController" || n.Contains("Camera") || n.Contains("Look"))
            {
                try { b.enabled = enable; } catch { }
            }
        }
    }

    public void OnQuitPressed()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
}
