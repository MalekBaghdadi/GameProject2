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
    /// Checks if the player has enough stamina to sustain sprinting.
    /// </summary>
    public bool CanSprint()
    {
        return playerStats.CurrentStamina > 0.1f; // Use a small threshold
    }

    /// <summary>
    /// Consumes stamina. Called repeatedly by PlayerController when the player is sprinting.
    /// </summary>
    public void ConsumeStamina()
    {
        float consumptionAmount = playerStats.StaminaConsumptionRate * Time.deltaTime;
        playerStats.ChangeStamina(-consumptionAmount);
        isConsuming = true;
    }

    /// <summary>
    /// Regenerates stamina. Called internally when the player is not consuming.
    /// </summary>
    private void RegenStamina()
    {
        float regenerationAmount = playerStats.StaminaRegenRate * Time.deltaTime;
        playerStats.ChangeStamina(regenerationAmount);
    }
}
