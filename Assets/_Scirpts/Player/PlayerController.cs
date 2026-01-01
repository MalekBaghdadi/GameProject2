using UnityEngine;
using System;
using Game.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour, ISaveable
{
    #region DataFields
    // --- SCRIPTABLE OBJECT REFERENCES ---
    [Tooltip("The Scriptable Object containing all player stats (Health, Speed, Stamina)")]
    [SerializeField] private PlayerStatsSO playerStats;
    
    // --- MOVEMENT SMOOTHING ---
    [Header("Movement Smoothing")]
    [Tooltip("The rate at which the character accelerates/decelerates (higher = faster response).")]
    [Range(1f, 10f)]
    public float moveSmoothTime = 5f;

    // --- CAMERA REFERENCES ---
    [Header("First-Person View")]
    [Tooltip("The camera component that rotates vertically.")]
    [SerializeField] private Transform playerCamera;
    public float mouseSensitivityX = 2.0f;
    public float mouseSensitivityY = 2.0f;
    public float verticalLookLimit = 85.0f;
    
    [Header("View Bob Settings")]
    public float bobAmplitude = 0.05f;
    public float bobFrequency = 12.0f;

    // --- COMPONENTS ---
    private CharacterController characterController;
    private StaminaController staminaController;
    public float sprintRecoveryThreshold = 0.3f;
    
    // --- MOVEMENT STATE ---
    private Vector3 currentVelocity; // The velocity applied to the character controller
    private Vector3 currentVelocitySmooth; // Reference for SmoothDamp to track velocity change
    private Vector3 externalGravityVector; // Separated for clear gravity application

    // --- INPUT VARIABLES (set by PlayerInput.cs) ---
    [NonSerialized] private Vector2 currentInput;
    [NonSerialized] public bool isSprinting;

    // --- VIEW STATE ---
    private float verticalRotation = 0f; // Stores vertical camera rotation
    private float bobTimer = 0.0f;
    private Vector3 cameraStartLocalPos;
    private float currentHorizontalSpeed; // Used for view bob calculations

    // --- CONSTANTS ---
    private const float Gravity = -9.81f * 3f; // Faster gravity for CC feel
    private const float GroundedGravity = -0.5f; // Ensures CharacterController remains grounded
    
    // Pause / Input gating fields
    [Header("Pause / Input gating")]
    private bool acceptInput = true;
    [SerializeField] private bool manageCursorOnPause = true; 
    [SerializeField] private CursorLockMode resumeLockMode = CursorLockMode.Locked;
    [SerializeField] private bool resumeCursorVisible = false;
    
    public Vector3 moveDirection;
    public HurtEffect hurtUI;
    
    public bool IsActuallySprinting
    {
        get
        {
            if (!isSprinting) return false;                    // player not holding sprint (input)
            if (staminaController == null) return false;      // no stamina system -> can't sprint
            // staminaController.CanSprint() already returns false if sprintLocked or low stamina
            return staminaController.CanSprint();
        }
    }
    
    #endregion
 

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        staminaController = GetComponent<StaminaController>();

        // Ensure components are present
        if (staminaController == null || playerCamera == null)
        {
            // Note: This error is expected if StaminaController is not yet attached to the GameObject.
            Debug.LogError("PlayerController is missing required component(s): StaminaController or PlayerCamera.");
        }
        
        playerStats.Initialize();
        
        // Initialize camera view settings
        if (playerCamera != null)
        {
            cameraStartLocalPos = playerCamera.localPosition;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        HandlePauseToggled();
    }

    private void OnEnable()
    {
        // Subscribe to relevant events
        EventManager.Subscribe(EventManager.ON_BEAR_ATTACK, OnBearAttack);
        // Persistence events (ON_GAME_LOADED, ON_REQUEST_SAVE) removed.
        if (UIManager.Instance != null)
            UIManager.Instance.OnPauseToggled += HandlePauseToggled;
        
        EventManager.Subscribe(EventManager.ON_PLAYER_DEATH, OnPlayerDeath);
        try { EventManager.Subscribe(EventManager.ON_PLAYER_RESPAWN, OnPlayerRespawn); } catch { }

    }

    private void OnDisable()
    {
        // Unsubscribe from events
        EventManager.Unsubscribe(EventManager.ON_BEAR_ATTACK, OnBearAttack);
        // Persistence events (ON_GAME_LOADED, ON_REQUEST_SAVE) removed.
        if (UIManager.Instance != null)
            UIManager.Instance.OnPauseToggled -= HandlePauseToggled;
        
        EventManager.Unsubscribe(EventManager.ON_PLAYER_DEATH, OnPlayerDeath);
        try { EventManager.Unsubscribe(EventManager.ON_PLAYER_RESPAWN, OnPlayerRespawn); } catch { }

    }
    
    private void Update()
    {
        if (!acceptInput) return;
        HandleCameraLook();
        HandleMovement();
        HandleGravity();
        HandleViewBob(); 
    }
    
    private void HandlePauseToggled()
    {
        bool isPaused = Mathf.Approximately(Time.timeScale, 0f);
        acceptInput = !isPaused;

        if (manageCursorOnPause)
        {
            if (isPaused)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = resumeLockMode;
                Cursor.visible = resumeCursorVisible;
            }
        }
    }

    // --- MOVEMENT LOGIC (Executed in Update) ---

    private void HandleMovement()
    {
        // --- 1. Calculate Target Velocity ---
        
        float speed = playerStats.BaseMovementSpeed;
        float sprintMultiplier = 1f;
        bool wantsToSprint = isSprinting;

        // Check if sprinting is possible and requested
        if (wantsToSprint && staminaController != null && playerStats != null)
        {
            float currentStamina = playerStats.CurrentStamina;
            float maxStamina = playerStats.MaxStamina;

            // 1A. Recovery Check: If stamina is not full, check if it's below the recovery threshold.
            // The check 'currentStamina < maxStamina' handles the initial run-out.
            if (staminaController != null)
            {
                // If sprint was locked due to full depletion, don't allow sprint until unlocked
                if (staminaController.IsSprintLocked)
                {
                    isSprinting = false;
                }
                else if (staminaController.CanSprint() && wantsToSprint)
                {
                    sprintMultiplier = playerStats.SprintMultiplier;
                    staminaController.ConsumeStamina();
                    isSprinting = true;
                }
                else
                {
                    isSprinting = false;
                }
            }
            else
            {
                // Fallback if no stamina controller: deny sprint
                isSprinting = false;
            }
        }
        else
        {
            // If the player isn't pressing the sprint button or controllers are missing, they are not sprinting.
            isSprinting = false; 
        }

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        // Calculate the desired direction based on input
        Vector3 desiredDirection = forward * currentInput.y + right * currentInput.x;
        
        // Set the target speed based on the determined state
        float targetSpeed = desiredDirection.magnitude > 0 ? speed * sprintMultiplier : 0f;
        
        // Final desired velocity vector
        Vector3 targetVelocity = desiredDirection.normalized * targetSpeed;

        // --- 2. Apply Smoothing ---

        // Use Vector3.SmoothDamp for high-quality acceleration/deceleration
        currentVelocity.x = Mathf.SmoothDamp(currentVelocity.x, targetVelocity.x, ref currentVelocitySmooth.x, 1f / moveSmoothTime);
        currentVelocity.z = Mathf.SmoothDamp(currentVelocity.z, targetVelocity.z, ref currentVelocitySmooth.z, 1f / moveSmoothTime);
        
        // --- 3. Move the Character (Horizontal Movement only) ---
        // Combine horizontal movement with the Y velocity (from HandleGravity)
        Vector3 finalMoveVector = new Vector3(currentVelocity.x, externalGravityVector.y, currentVelocity.z);
        characterController.Move(finalMoveVector * Time.deltaTime);
        // We use the velocity vector (ignoring gravity/vertical movement for footsteps usually)
        moveDirection = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        // Calculate current horizontal speed for view bob
        currentHorizontalSpeed = new Vector3(currentVelocity.x, 0, currentVelocity.z).magnitude;
    }

    private void HandleGravity()
    {
        if (characterController.isGrounded)
        {
            externalGravityVector.y = GroundedGravity; 
        }
        else
        {
            externalGravityVector.y += Gravity * Time.deltaTime;
        }
    }
    
    /// <summary>
    /// Handles camera rotation (looking around) using mouse input.
    /// </summary>
    private void HandleCameraLook()
    {
        if (playerCamera == null) return;
        
        // Horizontal Rotation (Character body rotates)
        float rotationX = Input.GetAxis("Mouse X") * mouseSensitivityX;
        transform.Rotate(0, rotationX, 0);

        // Vertical Rotation (Camera rotates)
        float rotationY = Input.GetAxis("Mouse Y") * mouseSensitivityY;
        verticalRotation -= rotationY;
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalLookLimit, verticalLookLimit);

        // Apply rotation to the camera
        playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }
    
    /// <summary>
    /// Applies subtle head bobbing to the camera for immersion.
    /// </summary>
    private void HandleViewBob()
    {
        if (playerCamera == null) return;
        
        // Only bob the camera if the character is grounded and moving
        if (characterController.isGrounded && currentHorizontalSpeed > 0.1f)
        {
            // Normalize speed factor based on the base walk speed
            float speedFactor = currentHorizontalSpeed / playerStats.BaseMovementSpeed;
            bobTimer += Time.deltaTime * bobFrequency * speedFactor;

            // Calculate horizontal and vertical offsets
            float horizontalOffset = Mathf.Cos(bobTimer * 0.5f) * bobAmplitude * speedFactor;
            float verticalOffset = Mathf.Sin(bobTimer) * bobAmplitude * speedFactor;

            // Apply the offsets to the camera's local position
            playerCamera.localPosition = cameraStartLocalPos + new Vector3(horizontalOffset, verticalOffset, 0f);
        }
        else
        {
            // Smoothly reset the camera position when standing still
            playerCamera.localPosition = Vector3.Lerp(playerCamera.localPosition, cameraStartLocalPos, Time.deltaTime * bobFrequency);
            // Reset timer to prevent sudden jumps in bob when starting to move again
            bobTimer = 0; 
        }
    }


    // --- PUBLIC INPUT HANDLERS (Called by PlayerInput.cs) ---

    public void HandleMoveInput(Vector2 input)
    {
        currentInput = input;
    }

    public void HandleSprint(bool sprinting)
    {
        isSprinting = sprinting;
    }

    // --- EVENT LISTENERS (Subscriber Actions) ---
    
    /// <summary>
    /// Subscriber method for the ON_BEAR_ATTACK event.
    /// This is how the Bear AI system damages the player without knowing the PlayerController's existence.
    /// </summary>
    /// <param name="data">Payload: [0] Transform attackerTransform (Optional)</param>
    private void OnBearAttack(object[] data)
    {
        // Default damage if not provided
        int damage = 10; 

        // Check if we received the damage amount in the event payload
        if (data != null && data.Length > 1 && data[1] is int damageAmount)
        {
            damage = damageAmount;
        }

        TakeDamage(damage);
        hurtUI.TriggerHurt();
    }
    
    /// <summary>
    /// Internal method to apply damage, which delegates the actual health change to the SO.
    /// </summary>
    public void TakeDamage(int amount)
    {
        playerStats.ChangeHealth(-amount);
    }
    
    private void OnPlayerDeath(object[] data)
    {
        // Disable ALL player input immediately
        acceptInput = false;

        // Unlock cursor so UI works
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    private void OnPlayerRespawn(object[] data)
    {
        // Re-enable Player input gating
        acceptInput = true;

        // Restore cursor lock/visibility for gameplay
        Cursor.lockState = resumeLockMode;
        Cursor.visible = resumeCursorVisible;
    }
    
    public void SaveData(ref GameData data)
    {
        data.playerPosition = transform.position;
        data.playerRotation = transform.rotation;
        data.playerVerticalLookRotation = verticalRotation;
    }
    // In PlayerController.cs

    public void LoadData(GameData data)
    {
        if (characterController != null) characterController.enabled = false;
    
        transform.position = data.playerPosition;
        transform.rotation = data.playerRotation;
        Physics.SyncTransforms();
    
        // Restore camera look angle
        verticalRotation = data.playerVerticalLookRotation;
        if (playerCamera != null) playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0, 0);

        // Re-enable the controller after a short delay
        Invoke(nameof(ReEnableController), 0.01f); 
    }

    private void ReEnableController()
    {
        if (characterController != null)
        {
            characterController.enabled = true;
        }
    }
}