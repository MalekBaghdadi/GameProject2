using UnityEngine;
using System;
// Removed System.Collections.Generic as it is no longer strictly needed

/// <summary>
/// Scriptable Object defining the player's permanent and runtime statistics.
/// This prevents hard-coded values and centralizes player configuration.
/// </summary>
[CreateAssetMenu(fileName = "PlayerStats", menuName = "GameData/Player Stats")]
public class PlayerStatsSO : ScriptableObject 
{
    // --- IMMUTABLE/CONFIG PROPERTIES ---
    [Header("Health")]
    public float MaxHealth = 100f;
    
    [Header("Movement")]
    public float BaseMovementSpeed = 5f;
    public float SprintMultiplier = 1.8f;
    
    [Header("Stamina")]
    public float MaxStamina = 100f;
    public float StaminaConsumptionRate = 15f; // Stamina lost per second while sprinting
    public float StaminaRegenRate = 10f; // Stamina gained per second when not sprinting

    // --- MUTABLE (RUNTIME) PROPERTIES ---
    [NonSerialized] private float currentHealth;
    [NonSerialized] private float currentStamina;

    public float CurrentHealth => currentHealth;
    public float CurrentStamina => currentStamina;

    /// <summary>
    /// Resets runtime stats to max values on game start or scene load.
    /// </summary>
    public void Initialize()
    {
        currentHealth = MaxHealth;
        currentStamina = MaxStamina;
    }

    /// <summary>
    /// Changes health and notifies all listeners via the Event Manager.
    /// This is the publishing action for health changes.
    /// </summary>
    /// <param name="amount">The change in health (negative for damage, positive for healing).</param>
    public void ChangeHealth(int amount)
    {
        float oldHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, MaxHealth);
        
        // 1. Trigger the event: Tell the world the health has changed.
        // Payload: [0] float newHealthValue
        EventManager.TriggerEvent(EventManager.ON_HEALTH_CHANGED, currentHealth);

        if (amount < 0)
        {
             // Fired specifically when damage is taken, useful for AudioManager (hit sound)
            EventManager.TriggerEvent(EventManager.ON_PLAYER_DAMAGED, (float)Mathf.Abs(amount));
        }

        if (currentHealth <= 0 && oldHealth > 0)
        {
            // Trigger death event only once
            EventManager.TriggerEvent(EventManager.ON_PLAYER_DEATH);
        }
    }
    
    /// <summary>
    /// Changes stamina and notifies all listeners.
    /// </summary>
    /// <param name="amount">The change in stamina (negative for consumption, positive for regen).</param>
    public void ChangeStamina(float amount)
    {
        currentStamina = Mathf.Clamp(currentStamina + amount, 0, MaxStamina);
        
        // Trigger the event: Tell the world the stamina has changed.
        // Payload: [0] float newStaminaValue
        EventManager.TriggerEvent(EventManager.ON_STAMINA_CHANGED, currentStamina);
    }
    
    // The LoadStats method for persistence has been removed.
}