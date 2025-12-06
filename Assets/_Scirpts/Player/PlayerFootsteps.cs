using UnityEngine;

public class PlayerFootsteps : MonoBehaviour
{
    [Header("Refs")]
    public CharacterController controller;
    public PlayerController playerMovement;
    public AudioSource footstepSource;

    [Header("Clips")]
    public AudioClip[] walkClips; 
    public AudioClip[] sprintClips;

    [Header("Rates")]
    [Tooltip("Time (seconds) between steps when walking at the player's BaseMovementSpeed.")]
    public float baseWalkRate = 0.55f;
    [Tooltip("Time (seconds) between steps when sprinting.")]
    public float baseSprintRate = 0.35f;

    [Header("Pitch Settings")]
    [Range(0.1f, 2f)]
    public float centerPitch = 1.0f; 
    public float pitchVariance = 0.1f; 

    [Header("Slope Settings")]
    [Tooltip("How long (seconds) to keep playing footsteps after losing ground contact. Fixes silence on bumpy slopes.")]
    public float groundGraceTime = 0.15f;

    private float stepTimer = 0f;
    private float timeSinceLastGrounded = 0f;
    
    // Safety constants
    private const float MIN_SPEED_FOR_RATE_CALC = 0.6f; 
    private const float BASE_PLAYER_WALK_SPEED = 5f; 

    void Update()
    {
        HandleFootsteps();
    }

    void HandleFootsteps()
    {
        // --- 1. HANDLE GROUND DETECTION (Slope Fix) ---
        // On slopes, isGrounded flickers. We use a timer to ignore those micro-flickers.
        if (controller.isGrounded)
        {
            timeSinceLastGrounded = 0f;
        }
        else
        {
            timeSinceLastGrounded += Time.deltaTime;
        }

        // If we have been in the air longer than the grace time, we are truly falling/jumping.
        // Stop logic here.
        if (timeSinceLastGrounded > groundGraceTime)
        {
            // Optional: You might want to reset the timer here so they step immediately upon landing
            // stepTimer = 0f; 
            return;
        }

        // --- 2. CHECK MOVEMENT ---
        // Check actual velocity magnitude (horizontal only)
        float currentHorizontalSpeed = playerMovement.moveDirection.magnitude;

        // Threshold to stop playing sound if standing still
        if (currentHorizontalSpeed <= 0.1f)
        {
            stepTimer = 0f; 
            return;
        }

        // --- 3. DETERMINE STATE (Sprint Fix) ---
        // Your PlayerController automatically sets isSprinting to false if stamina runs out,
        // so we trust this variable completely.
        bool isSprinting = playerMovement.isSprinting;

        float targetRate = isSprinting ? baseSprintRate : baseWalkRate;
        
        // --- 4. DYNAMIC RATE CALCULATION ---
        if (!isSprinting)
        {
             float clampedSpeed = Mathf.Max(currentHorizontalSpeed, MIN_SPEED_FOR_RATE_CALC);
             float speedRatio = BASE_PLAYER_WALK_SPEED / clampedSpeed;
             
             // Clamp ratio to keep steps from getting weirdly slow
             speedRatio = Mathf.Clamp(speedRatio, 1f, 2.0f);
             
             targetRate = baseWalkRate * speedRatio;
        }
        
        // --- 5. COUNTDOWN & PLAY ---
        stepTimer -= Time.deltaTime;
        
        if (stepTimer <= 0f)
        {
            PlayFootstep(isSprinting);
            // Reset timer to the calculated rate
            stepTimer = targetRate; 
        }
    }

    void PlayFootstep(bool isSprinting)
    {
        AudioClip[] clips = isSprinting ? sprintClips : walkClips;
        
        // Safety check if arrays are empty
        if (clips == null || clips.Length == 0) return;
        
        AudioClip clipToPlay = clips[Random.Range(0, clips.Length)];
        
        // Randomized pitch and volume
        footstepSource.pitch = centerPitch + Random.Range(-pitchVariance, pitchVariance);
        footstepSource.volume = Random.Range(0.85f, 1.0f); 
        
        footstepSource.PlayOneShot(clipToPlay);
    }
}