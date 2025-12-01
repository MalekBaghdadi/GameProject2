using UnityEngine;
using System;

public class DeathScreenActivator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the GameObject that has the DeathPanel script attached.")]
    public GameObject deathPanelGameObject; 

    // Internal reference we will find automatically
    private DeathPanel deathPanelComponent;

    void Awake()
    {
        // 1. Locate the DeathPanel component on the assigned GameObject
        if (deathPanelGameObject != null)
        {
            deathPanelComponent = deathPanelGameObject.GetComponent<DeathPanel>();
            
            if (deathPanelComponent == null)
            {
                Debug.LogError("DeathScreenActivator: The assigned 'deathPanelGameObject' does not have a 'DeathPanel' script attached to it!");
            }
        }
        else
        {
            Debug.LogWarning("DeathScreenActivator: You haven't assigned the Death Panel Game Object in the Inspector.");
        }

        // 2. Subscribe to the event
        // Note: Using EventManager.Instance ensures lazy initialization fixes the race condition
        EventManager.Subscribe(EventManager.ON_PLAYER_DEATH, OnPlayerDeathEvent);
    }

    void OnDisable()
    {
        EventManager.Unsubscribe(EventManager.ON_PLAYER_DEATH, OnPlayerDeathEvent);
    }

    private void OnPlayerDeathEvent(object[] data)
    {
        // Debug log to confirm this part of the code is reached
        if (deathPanelComponent != null)
        {
            // 3. Guarantee the host GameObject is active (so coroutines can run)
            if (!deathPanelComponent.gameObject.activeSelf)
            {
                deathPanelComponent.gameObject.SetActive(true);
            }
            
            // 4. Call the public method to start the UI sequence
            deathPanelComponent.ShowDeath();
        }
        else
        {
            Debug.LogError("DeathScreenActivator: Cannot show death screen because DeathPanel component was not found!");
        }
    }
}