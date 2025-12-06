using UnityEngine;
using System;

/// <summary>
/// Manages the consumption and regeneration of player stamina based on PlayerStatsSO data.
/// Works with PlayerController to enable/disable sprinting.
/// </summary>
public class StaminaController : MonoBehaviour
{
    [SerializeField] private PlayerStatsSO playerStats;
    [Tooltip("Reference to the PlayerController to check if movement is active.")]
    [SerializeField] private PlayerController playerController;

    private bool isConsuming = false;

    // --- NEW: sprint lock state (true = sprint disabled until recovery threshold) ---
    private bool sprintLocked = false;
    public bool IsSprintLocked => sprintLocked;
    // ---------------------------------------------------------------------------

    private void Awake()
    {
        // fallback if reference not assigned in inspector
        if (playerController == null)
            playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        // If the PlayerController is not actively consuming stamina (i.e., not sprinting),
        // we handle regeneration here.
        if (!isConsuming && playerStats.CurrentStamina < playerStats.MaxStamina)
        {
            RegenStamina();
        }

        // Reset consumption state every frame, only set to true by ConsumeStamina() called from PlayerController
        isConsuming = false;
    }

    /// <summary>
    /// Checks if the player has enough stamina to sustain sprinting and whether sprint is unlocked.
    /// </summary>
    public bool CanSprint()
    {
        // If sprint is locked because we fully depleted, check whether we can unlock now
        if (sprintLocked)
        {
            // require playerController available and threshold value
            float threshold = (playerController != null) ? playerController.sprintRecoveryThreshold : 0.3f;
            if ((playerStats.CurrentStamina / playerStats.MaxStamina) >= threshold)
            {
                sprintLocked = false; // unlock
                return playerStats.CurrentStamina > 0.01f;
            }
            return false;
        }

        // Normal check (not locked)
        return playerStats.CurrentStamina > 0.01f;
    }

    /// <summary>
    /// Consumes stamina. Called repeatedly by PlayerController when the player is sprinting.
    /// </summary>
    public void ConsumeStamina()
    {
        float consumptionAmount = playerStats.StaminaConsumptionRate * Time.deltaTime;
        playerStats.ChangeStamina(-consumptionAmount);
        isConsuming = true;

        // If we reach zero (or below small epsilon), clamp and lock sprint
        if (playerStats.CurrentStamina <= 0f)
        {
            playerStats.ChangeStamina(-playerStats.CurrentStamina); // assume PlayerStatsSO exposes a SetStamina; if not, ChangeStamina already clamped. Replace with clamp if needed.
            sprintLocked = true;
        }
    }

    /// <summary>
    /// Regenerates stamina. Called internally when the player is not consuming.
    /// Also checks for auto-unlock when threshold reached.
    /// </summary>
    private void RegenStamina()
    {
        float regenerationAmount = playerStats.StaminaRegenRate * Time.deltaTime;
        playerStats.ChangeStamina(regenerationAmount);

        // If locked, check if regen reached recovery threshold -> unlock
        if (sprintLocked && playerController != null)
        {
            float ratio = playerStats.CurrentStamina / playerStats.MaxStamina;
            if (ratio >= playerController.sprintRecoveryThreshold)
            {
                sprintLocked = false;
            }
        }
    }
}
