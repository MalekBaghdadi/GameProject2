using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralized Event Manager implementing the Observer Pattern.
/// All systems subscribe to and trigger events via this manager to achieve
/// loose coupling and separation of concerns.
/// </summary>
public class EventManager : MonoBehaviour
{
    // --- STATIC EVENT CONSTANTS (The "Contract") ---
    // These strings define the public API for all systems to communicate.

    // 1. Game State Events (Published by GameManager/SceneLoader)
    public const string ON_GAME_START = "OnGameStart";
    public const string ON_GAME_PAUSE = "OnGamePause";
    public const string ON_GAME_RESUME = "OnGameResume";
    public const string ON_GAME_OVER = "OnGameOver";

    // 2. Player & Stats Events (Published by PlayerStatsSO/PlayerController)
    // Payload: [0] float newHealthValue
    public const string ON_HEALTH_CHANGED = "OnHealthChanged";
    // Payload: [0] float newStaminaValue
    public const string ON_STAMINA_CHANGED = "OnStaminaChanged";
    // Payload: [0] int damageAmount
    public const string ON_PLAYER_DAMAGED = "OnPlayerDamaged";
    public const string ON_PLAYER_DEATH = "OnPlayerDeath";
    // Payload: no payload. Fired after the player has been respawned (used by listeners to restore stats, UI, etc.)
    public const string ON_PLAYER_RESPAWN = "OnPlayerRespawn";

    // 3. Inventory & World Events (Published by Collectible/CabinRepairController)
    // Payload: [0] ItemDataSO collectedItem
    public const string ON_ITEM_COLLECTED = "OnItemCollected";
    // Payload: [0] string nextSceneName
    public const string ON_CABIN_REPAIRED = "OnCabinRepaired";

    // 4. AI Events (Published by BearAIController/FoxHelperController)
    // Payload: [0] Transform attackerTransform (Optional)
    public const string ON_BEAR_ATTACK = "OnBearAttack";
    // Payload: [0] Vector3 itemWorldPosition
    public const string ON_FOX_HINT_START = "OnFoxHintStart";
    public const string ON_FOX_HINT_END = "OnFoxHintEnd";

    // 5. Audio Events (Published by LevelConfigSO or specific components)
    // Payload: [0] AudioClip clipToPlay
    public const string ON_PLAY_SFX = "OnPlaySFX";
    // Payload: [0] AudioClip clipToPlay
    public const string ON_PLAY_BGM = "OnPlayBGM";

    // --- SINGLETON IMPLEMENTATION ---
    private static EventManager instance;
    public static EventManager Instance
    {
        get
        {
            if (instance == null)
            {
                // Find existing instance or create a new GameObject for it
                instance = FindObjectOfType<EventManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("EventManager");
                    instance = go.AddComponent<EventManager>();
                }
                DontDestroyOnLoad(instance.gameObject);
            }
            instance.Init();
            return instance;
        }
    }

    // --- CORE MECHANICS ---
    // The dictionary holds the list of listeners (Actions) mapped to a specific event name (string).
    private Dictionary<string, Action<object[]>> eventDictionary;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            Init();
            DontDestroyOnLoad(gameObject);
            eventDictionary = new Dictionary<string, Action<object[]>>();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Safe initialization method that checks if dictionary exists before creating it.
    /// </summary>
    private void Init()
    {
        if (eventDictionary == null)
        {
            eventDictionary = new Dictionary<string, Action<object[]>>();
        }
    }

    /// <summary>
    /// Registers a listener method to be called when a specific event is triggered.
    /// </summary>
    /// <param name="eventName">The name of the event (use the constants above).</param>
    /// <param name="listener">The method to be executed (Action<object[]>).</param>
    public static void Subscribe(string eventName, Action<object[]> listener)
    {
        if (Instance.eventDictionary.TryGetValue(eventName, out Action<object[]> thisEvent))
        {
            thisEvent += listener;
            Instance.eventDictionary[eventName] = thisEvent;
        }
        else
        {
            thisEvent += listener;
            Instance.eventDictionary.Add(eventName, thisEvent);
        }
    }

    /// <summary>
    /// Removes a listener method from an event.
    /// </summary>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="listener">The method to be removed.</param>
    public static void Unsubscribe(string eventName, Action<object[]> listener)
    {
        if (instance == null) return;

        if (Instance.eventDictionary.TryGetValue(eventName, out Action<object[]> thisEvent))
        {
            thisEvent -= listener;
            Instance.eventDictionary[eventName] = thisEvent;
        }
    }

    /// <summary>
    /// Triggers an event, executing all registered listeners.
    /// </summary>
    /// <param name="eventName">The name of the event to trigger.</param>
    /// <param name="data">Optional array of objects to pass as payload to listeners.</param>
    public static void TriggerEvent(string eventName, params object[] data)
    {
        if (Instance.eventDictionary.TryGetValue(eventName, out Action<object[]> thisEvent))
        {
            // The null check is crucial as a listener might be null if a GameObject was destroyed
            // without properly unsubscribing.
            thisEvent?.Invoke(data); 
        }
        #if UNITY_EDITOR
        // Optional: Log a warning if a non-existent event is triggered in the editor
        else
        {
            // Reduce noise: only log if it's a critical event like Death
            if(eventName == ON_PLAYER_DEATH) 
                Debug.LogWarning($"EventManager: Event '{eventName}' triggered but has no active listeners.");
        }
        #endif
    }
}