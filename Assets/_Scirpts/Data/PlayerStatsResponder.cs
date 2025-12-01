using UnityEngine;

/// <summary>
/// Listens for ON_PLAYER_RESPAWN and restores player stats via the PlayerStatsSO API.
/// This keeps the respawn -> stats flow event-driven and ensures health/stamina change events are emitted by the SO.
/// </summary>
public class PlayerStatsResponder : MonoBehaviour
{
    [Tooltip("Reference to the PlayerStatsSO used at runtime (assign in inspector).")]
    public PlayerStatsSO playerStats;

    // Optional: if you want to restore a fraction rather than full, expose multiplier
    [Range(0f, 1f)]
    public float restoreFraction = 1f; // 1 = full restore

    void OnEnable()
    {
        EventManager.Subscribe("ON_PLAYER_RESPAWN", OnPlayerRespawnEvent);
    }

    void OnDisable()
    {
        EventManager.Unsubscribe("ON_PLAYER_RESPAWN", OnPlayerRespawnEvent);
    }

    void OnPlayerRespawnEvent(object[] data)
    {
        if (playerStats == null)
        {
            Debug.LogWarning("PlayerStatsResponder: playerStats not assigned.");
            return;
        }

        // Calculate heal amount (use int signature expected by ChangeHealth)
        float desiredHealth = playerStats.MaxHealth * restoreFraction;
        int healAmount = Mathf.CeilToInt(desiredHealth - playerStats.CurrentHealth);
        if (healAmount > 0)
        {
            playerStats.ChangeHealth(healAmount); // triggers ON_HEALTH_CHANGED internally
        }

        // Restore stamina (ChangeStamina accepts float)
        float desiredStamina = playerStats.MaxStamina * restoreFraction;
        float staminaDelta = desiredStamina - playerStats.CurrentStamina;
        if (staminaDelta > 0f)
        {
            playerStats.ChangeStamina(staminaDelta); // triggers ON_STAMINA_CHANGED internally
        }
    }
}