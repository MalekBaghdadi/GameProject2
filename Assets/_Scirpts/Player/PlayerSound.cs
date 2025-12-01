using UnityEngine;

public class PlayerFootsteps : MonoBehaviour
{
    [Header("Refs")]
    public CharacterController controller;
    public PlayerController playerMovement;
    public AudioSource footstepSource;

    [Header("Clips")]
    // Make sure you assign multiple clips here for variety!
    public AudioClip[] walkClips; 
    public AudioClip[] sprintClips;

    [Header("Rates")]
    [Tooltip("Time (seconds) between steps when walking at the player's BaseMovementSpeed.")]
    public float baseWalkRate = 0.55f;
    [Tooltip("Time (seconds) between steps when sprinting.")]
    public float baseSprintRate = 0.35f;

    [Header("Pitch Settings")]
    [Tooltip("Center pitch for sound.")]
    [Range(0.1f, 2f)]
    public float centerPitch = 1.0f; 

    [Tooltip("How much the pitch randomly varies per step (e.g., 0.1 means +/- 0.1).")]
    public float pitchVariance = 0.1f; 

    private float stepTimer = 0f;
    private float currentStepRate = 0f;
    
    // Safety: Prevents division by near-zero, ensuring the slowest step isn't too long.
    private const float MIN_SPEED_FOR_RATE_CALC = 0.6f; 
    
    // We must assume the BaseMovementSpeed is 5f for the ratio calculation (based on your PlayerStatsSO)
    private const float BASE_PLAYER_WALK_SPEED = 5f; 

    void Update()
    {
        HandleFootsteps();
    }

    void HandleFootsteps()
    {
        float currentHorizontalSpeed = playerMovement.moveDirection.magnitude;
        
        // Player must be moving and grounded. Use a 0.1f threshold.
        bool isMoving = currentHorizontalSpeed > 0.1f && controller.isGrounded;

        if (!isMoving)
        {
            stepTimer = 0f; 
            currentStepRate = 0f;
            return;
        }

        bool isSprinting = playerMovement.isSprinting;
        
        // 1. Determine the initial target step rate
        float targetRate = isSprinting ? baseSprintRate : baseWalkRate;
        
        // 2. Adjust the step rate dynamically based on actual speed (only for walking)
        if (!isSprinting)
        {
             // Use Math.Max to ensure we never divide by a value less than the minimum sensible speed.
             float clampedSpeed = Mathf.Max(currentHorizontalSpeed, MIN_SPEED_FOR_RATE_CALC);
             
             // Calculate the ratio: speed is lower -> rate is higher (longer time between steps).
             float speedRatio = BASE_PLAYER_WALK_SPEED / clampedSpeed;
             
             // Cap the step time to prevent massive values (e.g., slowest step is 2x baseWalkRate)
             speedRatio = Mathf.Clamp(speedRatio, 1f, 2.0f);
             
             targetRate = baseWalkRate * speedRatio;
        }
        
        currentStepRate = targetRate;

        stepTimer -= Time.deltaTime;
        
        if (stepTimer <= 0f)
        {
            PlayFootstep(isSprinting);
            // RESET: The timer is correctly reset to the calculated duration.
            stepTimer = currentStepRate; 
        }
    }

    void PlayFootstep(bool isSprinting)
    {
        AudioClip[] clips = isSprinting ? sprintClips : walkClips;
        
        if (clips == null || clips.Length == 0) return;
        
        // 1. Randomly select a clip for variety
        AudioClip clipToPlay = clips[Random.Range(0, clips.Length)];
        
        // 2. Calculate randomized pitch
        float pitch = centerPitch + Random.Range(-pitchVariance, pitchVariance);
        
        // 3. Apply and Play
        footstepSource.pitch = pitch;
        footstepSource.volume = Random.Range(0.85f, 1.0f); // Random volume for natural feel
        
        footstepSource.PlayOneShot(clipToPlay);
    }
}