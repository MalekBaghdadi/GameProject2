using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Simple respawn helper:
/// - Teleports the player to respawnPoint (or player tag 'Player' if not assigned).
/// - Safely toggles CharacterController when teleporting to avoid physics glitches.
/// - Optionally re-enables listed player control components.
/// - Invokes onRespawnComplete UnityEvent and triggers EventManager.ON_PLAYER_RESPAWN.
/// 
/// Usage:
/// - Attach to a manager object (e.g., GameManagers).
/// - Assign respawnPoint and (optionally) player and playerControlComponents in inspector.
/// - In DeathPanel.onRespawn (Inspector) add this object's Respawn() method.
/// </summary>
public class RespawnManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Player GameObject (optional). If empty, will try GameObject.FindWithTag(\"Player\").")]
    public GameObject player;

    [Tooltip("Transform to teleport the player to on respawn.")]
    public Transform respawnPoint;

    [Header("Control toggles")]
    [Tooltip("Components on the player to re-enable after respawn (recommended: PlayerController, MouseLook)")]
    public MonoBehaviour[] playerControlComponents;

    [Header("Options")]
    [Tooltip("Whether to automatically re-enable the CharacterController after teleport.")]
    public bool resetCharacterController = true;

    [Header("Callbacks")]
    public UnityEvent onRespawnComplete;

    // Public API: call this to respawn immediately.
    public void Respawn()
    {
        StartCoroutine(RespawnCoroutine());
    }

    IEnumerator RespawnCoroutine()
    {
        // Resolve player
        if (player == null)
        {
            player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("RespawnManager: Player not assigned and tag 'Player' not found. Aborting respawn.");
                yield break;
            }
        }

        if (respawnPoint == null)
        {
            Debug.LogWarning("RespawnManager: respawnPoint not assigned. Aborting respawn.");
            yield break;
        }

        // Try to disable controls briefly to avoid conflicts during teleport
        bool[] prevStates = null;
        if (playerControlComponents != null && playerControlComponents.Length > 0)
        {
            prevStates = new bool[playerControlComponents.Length];
            for (int i = 0; i < playerControlComponents.Length; i++)
            {
                var comp = playerControlComponents[i];
                if (comp == null) continue;
                prevStates[i] = comp.enabled;
                comp.enabled = false;
            }
        }

        // CharacterController safe-teleport
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null && resetCharacterController)
        {
            cc.enabled = false;
            // Wait a frame to ensure physics state updated (safe)
            yield return null;
        }

        // Teleport player to respawn point (world pos & rot)
        player.transform.position = respawnPoint.position;
        player.transform.rotation = respawnPoint.rotation;

        // Small frame wait to settle transforms
        yield return null;

        if (cc != null && resetCharacterController)
        {
            cc.enabled = true;
            // wait one frame for CharacterController to reinitialize
            yield return null;
        }

        // Restore controls
        if (playerControlComponents != null && playerControlComponents.Length > 0)
        {
            for (int i = 0; i < playerControlComponents.Length; i++)
            {
                var comp = playerControlComponents[i];
                if (comp == null) continue;
                if (prevStates != null && i < prevStates.Length) comp.enabled = prevStates[i];
                else comp.enabled = true;
            }
        }
        // Fire UnityEvent for inspector wiring
        onRespawnComplete?.Invoke();
        // Fire EventManager event if present (do this last)
        try { EventManager.TriggerEvent(EventManager.ON_PLAYER_RESPAWN); } 
        catch { Debug.LogWarning("RespawnManager: Trigger event failed"); }
        // Fire UnityEvent for inspector wiring
        onRespawnComplete?.Invoke();
        // Fire EventManager event if present
        try { EventManager.TriggerEvent("ON_PLAYER_RESPAWN"); } catch { }

        yield break;
    }
}