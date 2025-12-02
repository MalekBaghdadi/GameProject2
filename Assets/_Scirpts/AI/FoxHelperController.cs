using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Controls the Fox companion AI.
/// Behavior:
/// 1. Defaults to wandering loosely near the player.
/// 2. When receiving ON_FOX_HINT_START, moves to the specific item location.
/// 3. Returns to player when receiving ON_FOX_HINT_END or when item is collected.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class FoxHelperController : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("How close to the player the fox tries to stay when wandering.")]
    [SerializeField] private float wanderRadius = 5f;
    [Tooltip("How often the fox picks a new spot near the player.")]
    [SerializeField] private float wanderInterval = 3f;
    [Tooltip("Stopping distance when guiding to an item.")]
    [SerializeField] private float guideStoppingDistance = 1.5f;

    // --- COMPONENTS ---
    private NavMeshAgent navMeshAgent;
    private Animator animator;

    // --- STATE ---
    private Transform playerTransform;
    private FoxState currentState = FoxState.WanderNearPlayer;
    private Vector3 currentItemTarget;
    private Coroutine wanderCoroutine;

    private enum FoxState
    {
        WanderNearPlayer,
        GuidingToItem
    }

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogError("FoxHelperController: Could not find object with tag 'Player'.");
        }
    }

    private void OnEnable()
    {
        // Subscribe to events defining the behavior
        EventManager.Subscribe(EventManager.ON_FOX_HINT_START, OnHintStart);
        EventManager.Subscribe(EventManager.ON_FOX_HINT_END, OnHintEnd);
        EventManager.Subscribe(EventManager.ON_ITEM_COLLECTED, OnItemCollected);
    }

    private void OnDisable()
    {
        EventManager.Unsubscribe(EventManager.ON_FOX_HINT_START, OnHintStart);
        EventManager.Unsubscribe(EventManager.ON_FOX_HINT_END, OnHintEnd);
        EventManager.Unsubscribe(EventManager.ON_ITEM_COLLECTED, OnItemCollected);
    }

    private void Start()
    {
        // Start the default behavior
        StartWandering();
    }

    private void Update()
    {
        // Update Animation parameters based on agent velocity
        //float speed = navMeshAgent.velocity.magnitude;
        //animator.SetFloat("Speed", speed);

        // State-specific update logic (if needed beyond Coroutines/NavMesh)
        if (currentState == FoxState.GuidingToItem)
        {
            // Ensure the fox looks at the item when it arrives
            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                // Optional: Play a "bark" or "sit" animation here
            }
        }
    }

    // --- BEHAVIOR LOGIC ---

    private void StartWandering()
    {
        currentState = FoxState.WanderNearPlayer;
        navMeshAgent.stoppingDistance = 0f; // Reset default
        
        if (wanderCoroutine != null) StopCoroutine(wanderCoroutine);
        wanderCoroutine = StartCoroutine(WanderRoutine());
    }

    private IEnumerator WanderRoutine()
    {
        while (currentState == FoxState.WanderNearPlayer)
        {
            if (playerTransform != null)
            {
                // Find a random point near the player
                Vector3 randomPoint = RandomNavSphere(playerTransform.position, wanderRadius, -1);
                navMeshAgent.SetDestination(randomPoint);
            }
            
            // Wait before picking a new spot
            yield return new WaitForSeconds(wanderInterval);
        }
    }

    /// <summary>
    /// Helper to find a random point on the NavMesh within a radius.
    /// </summary>
    private static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);
        return navHit.position;
    }

    private void GoToItem(Vector3 targetPos)
    {
        // Stop wandering logic
        if (wanderCoroutine != null) StopCoroutine(wanderCoroutine);
        
        currentState = FoxState.GuidingToItem;
        currentItemTarget = targetPos;
        
        navMeshAgent.stoppingDistance = guideStoppingDistance;
        navMeshAgent.SetDestination(targetPos);
    }

    // --- EVENT LISTENERS ---

    /// <summary>
    /// Triggered when Player enters a FoxHintTrigger zone.
    /// Payload: [0] Vector3 itemPosition
    /// </summary>
    private void OnHintStart(object[] data)
    {
        if (data.Length > 0 && data[0] is Vector3 itemPos)
        {
            GoToItem(itemPos);
        }
    }

    /// <summary>
    /// Triggered when Player leaves a FoxHintTrigger zone.
    /// </summary>
    private void OnHintEnd(object[] data)
    {
        // Only switch back if we are currently guiding
        if (currentState == FoxState.GuidingToItem)
        {
            StartWandering();
        }
    }

    /// <summary>
    /// Triggered when ANY item is collected.
    /// If the fox was guiding to this specific item (or just generally), reset to wander.
    /// </summary>
    private void OnItemCollected(object[] data)
    {
        // If we were guiding, we assume the job is done regardless of which item it was
        // (Simpler logic, prevents fox staring at empty space)
        if (currentState == FoxState.GuidingToItem)
        {
            StartWandering();
        }
    }
}