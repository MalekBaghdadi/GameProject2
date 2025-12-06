using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Enhanced Fox companion AI - A memory guide that discovers and marks collectible items.
/// 
/// NEW BEHAVIORS:
/// - Autonomously detects nearby collectibles and investigates them
/// - Barks/signals when finding an item to alert the player
/// - Stays at discovered items until player approaches or moves too far away
/// - Warns player when bear is dangerously close (protective behavior)
/// - Returns to player's side when sensing danger
/// - Remembers which items it has already shown the player
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class FoxHelperController : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("How close to the player the fox tries to stay when wandering.")]
    [SerializeField] private float wanderRadius = 8f;
    [Tooltip("How often the fox picks a new spot near the player.")]
    [SerializeField] private float wanderInterval = 4f;
    [Tooltip("How far the fox can detect collectible items.")]
    [SerializeField] private float itemDetectionRadius = 15f;
    [Tooltip("How close fox gets to an item before stopping.")]
    [SerializeField] private float itemStoppingDistance = 1.2f;
    [Tooltip("How far player must be before fox abandons showing an item.")]
    [SerializeField] private float playerAbandonDistance = 25f;
    [Tooltip("How close player must get to item before fox considers it 'shown'.")]
    [SerializeField] private float playerItemProximity = 5f;
    
    [Header("Bear Warning System")]
    [Tooltip("How far the fox can sense the bear approaching.")]
    [SerializeField] private float bearDetectionRadius = 20f;
    [Tooltip("How close bear must be to trigger urgent retreat to player.")]
    [SerializeField] private float bearDangerRadius = 12f;
    [Tooltip("Time between bark warnings about the bear.")]
    [SerializeField] private float bearWarningCooldown = 5f;

    [Header("Behavior Tuning")]
    [Tooltip("Time fox waits at an item before considering it shown (even if player is far).")]
    [SerializeField] private float itemShowTimeout = 20f;
    [Tooltip("Fox won't investigate items it has already shown for this many seconds.")]
    [SerializeField] private float itemMemoryCooldown = 45f;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip barkItemFound;
    [SerializeField] private AudioClip barkBearWarning;
    [SerializeField] private AudioClip whimperRetreat;
    private AudioSource audioSource;

    // --- COMPONENTS ---
    private NavMeshAgent navMeshAgent;
    private Animator animator;

    // --- STATE ---
    private Transform playerTransform;
    private Transform bearTransform;
    private FoxState currentState = FoxState.WanderNearPlayer;
    
    private GameObject currentTargetItem; // The item fox is currently showing
    private Vector3 currentItemPosition;
    private float timeAtCurrentItem = 0f;
    
    private Dictionary<GameObject, float> shownItems = new Dictionary<GameObject, float>(); // Item -> time shown
    
    private Coroutine wanderCoroutine;
    private float lastBearWarningTime = -999f;

    private enum FoxState
    {
        WanderNearPlayer,      // Default: exploring near player
        InvestigatingItem,     // Moving toward a discovered item
        ShowingItem,           // Arrived at item, waiting for player
        FleeingFromBear,       // Retreating to player due to bear
        GuidingToItem          // Manual hint trigger (kept for compatibility)
    }

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogError("FoxHelperController: Could not find object with tag 'Player'.");
        }

        // Try to find the bear in the scene
        GameObject bearObj = GameObject.FindGameObjectWithTag("Enemy"); // Or "Bear" if you use that tag
        if (bearObj != null)
        {
            bearTransform = bearObj.transform;
        }
    }

    private void OnEnable()
    {
        // Subscribe to events
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
        StartWandering();
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // Update animation based on speed
        float speed = navMeshAgent.velocity.magnitude;
        if (animator != null)
        {
            animator.SetFloat("Speed", speed);
        }

        // Check for bear proximity (always active)
        CheckBearProximity();

        // Clean up old shown items from memory
        CleanShownItemsMemory();

        // State machine
        switch (currentState)
        {
            case FoxState.WanderNearPlayer:
                WanderUpdate();
                break;
            case FoxState.InvestigatingItem:
                InvestigatingUpdate();
                break;
            case FoxState.ShowingItem:
                ShowingItemUpdate();
                break;
            case FoxState.FleeingFromBear:
                FleeingUpdate();
                break;
            case FoxState.GuidingToItem:
                GuidingUpdate();
                break;
        }
    }

    // --- BEAR DETECTION SYSTEM ---

    private void CheckBearProximity()
    {
        if (bearTransform == null) return;

        float distToBear = Vector3.Distance(transform.position, bearTransform.position);
        float distPlayerToBear = Vector3.Distance(playerTransform.position, bearTransform.position);

        // If bear is dangerously close to player, fox retreats to player's side
        if (distPlayerToBear <= bearDangerRadius)
        {
            if (currentState != FoxState.FleeingFromBear)
            {
                StartFleeingFromBear();
            }
        }
        // If bear is in detection range, warn player occasionally
        else if (distToBear <= bearDetectionRadius || distPlayerToBear <= bearDetectionRadius)
        {
            if (Time.time - lastBearWarningTime >= bearWarningCooldown)
            {
                WarnAboutBear();
                lastBearWarningTime = Time.time;
            }
        }
    }

    private void StartFleeingFromBear()
    {
        if (wanderCoroutine != null) StopCoroutine(wanderCoroutine);
        
        currentState = FoxState.FleeingFromBear;
        navMeshAgent.stoppingDistance = 2f;
        navMeshAgent.speed = navMeshAgent.speed * 1.3f; // Run faster when scared!
        
        PlaySound(whimperRetreat);
        
        // Optional: Trigger fear animation
        if (animator != null)
        {
            animator.SetTrigger("Fear");
        }
    }

    private void WarnAboutBear()
    {
        // Bark to warn player
        PlaySound(barkBearWarning);
        
        // Look toward bear briefly
        if (bearTransform != null)
        {
            Vector3 lookDir = bearTransform.position - transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
        
        // Optional: Trigger bark animation
        if (animator != null)
        {
            animator.SetTrigger("Bark");
        }
    }

    private void FleeingUpdate()
    {
        // Stay close to player
        navMeshAgent.SetDestination(playerTransform.position);

        // Check if danger has passed
        if (bearTransform != null)
        {
            float distToBear = Vector3.Distance(playerTransform.position, bearTransform.position);
            if (distToBear > bearDangerRadius + 5f) // Add buffer before calming down
            {
                // Danger passed, resume normal behavior
                navMeshAgent.speed = navMeshAgent.speed / 1.3f; // Return to normal speed
                StartWandering();
            }
        }
    }

    // --- WANDERING & ITEM DETECTION ---

    private void StartWandering()
    {
        currentState = FoxState.WanderNearPlayer;
        navMeshAgent.stoppingDistance = 0.5f;
        navMeshAgent.speed = 3.5f; // Default wander speed
        
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
            
            yield return new WaitForSeconds(wanderInterval);
        }
    }

    private void WanderUpdate()
    {
        // Scan for nearby collectible items while wandering
        GameObject nearestItem = FindNearestCollectible();
        
        if (nearestItem != null)
        {
            // Found something! Go investigate
            StartInvestigatingItem(nearestItem);
        }
    }

    private GameObject FindNearestCollectible()
    {
        // Find all objects with "Collectible" tag within detection radius
        GameObject[] collectibles = GameObject.FindGameObjectsWithTag("Collectible");
        
        GameObject nearest = null;
        float nearestDist = itemDetectionRadius;

        foreach (GameObject item in collectibles)
        {
            // Skip if we've already shown this item recently
            if (shownItems.ContainsKey(item))
            {
                float timeSinceShown = Time.time - shownItems[item];
                if (timeSinceShown < itemMemoryCooldown)
                    continue;
            }

            float dist = Vector3.Distance(transform.position, item.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = item;
            }
        }

        return nearest;
    }

    // --- INVESTIGATING & SHOWING ITEMS ---

    private void StartInvestigatingItem(GameObject item)
    {
        if (wanderCoroutine != null) StopCoroutine(wanderCoroutine);
        
        currentState = FoxState.InvestigatingItem;
        currentTargetItem = item;
        currentItemPosition = item.transform.position;
        
        navMeshAgent.stoppingDistance = itemStoppingDistance;
        navMeshAgent.speed = 4.5f; // Move a bit faster when investigating
        navMeshAgent.SetDestination(currentItemPosition);
    }

    private void InvestigatingUpdate()
    {
        if (currentTargetItem == null)
        {
            // Item was collected or destroyed
            StartWandering();
            return;
        }

        // Check if we've arrived at the item
        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
        {
            ArriveAtItem();
        }

        // Check if player moved too far away - abandon this item
        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distToPlayer > playerAbandonDistance)
        {
            StartWandering();
        }
    }

    private void ArriveAtItem()
    {
        currentState = FoxState.ShowingItem;
        timeAtCurrentItem = 0f;
        
        // Bark to alert player!
        PlaySound(barkItemFound);
        
        // Trigger excited animation
        if (animator != null)
        {
            animator.SetTrigger("FoundItem");
        }

        // Look at the item
        Vector3 lookDir = currentItemPosition - transform.position;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(lookDir);
        }

        // Optional: Trigger visual effect (particle system, glow, etc.)
        HighlightItem(currentTargetItem);
    }

    private void ShowingItemUpdate()
    {
        if (currentTargetItem == null)
        {
            // Item was collected
            StartWandering();
            return;
        }

        timeAtCurrentItem += Time.deltaTime;

        // Stay at item, occasionally look at it and bark
        if (Mathf.FloorToInt(timeAtCurrentItem) % 8 == 0 && Time.deltaTime > 0) // Every ~8 seconds
        {
            Vector3 lookDir = currentItemPosition - transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
            
            if (animator != null)
            {
                animator.SetTrigger("Bark");
            }
        }

        // Check if player came close enough to the item
        float playerDistToItem = Vector3.Distance(playerTransform.position, currentItemPosition);
        if (playerDistToItem <= playerItemProximity)
        {
            // Player saw the item! Mark it as shown
            MarkItemAsShown(currentTargetItem);
            StartWandering();
            return;
        }

        // Check if player moved too far away - abandon
        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distToPlayer > playerAbandonDistance)
        {
            StartWandering();
            return;
        }

        // Timeout - fox gives up after showing for too long
        if (timeAtCurrentItem >= itemShowTimeout)
        {
            MarkItemAsShown(currentTargetItem);
            StartWandering();
        }
    }

    private void MarkItemAsShown(GameObject item)
    {
        if (item != null)
        {
            shownItems[item] = Time.time;
            UnhighlightItem(item);
        }
    }

    private void CleanShownItemsMemory()
    {
        // Remove items from memory after cooldown expires
        List<GameObject> toRemove = new List<GameObject>();
        
        foreach (var kvp in shownItems)
        {
            if (kvp.Key == null || Time.time - kvp.Value >= itemMemoryCooldown)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var item in toRemove)
        {
            shownItems.Remove(item);
        }
    }

    // --- MANUAL HINT SYSTEM (Compatibility) ---

    private void GuidingUpdate()
    {
        // Similar to ShowingItem but triggered manually via events
        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
        {
            // Optional: Play animation at destination
            if (animator != null)
            {
                animator.SetBool("IsGuiding", true);
            }
        }
    }

    private void GoToItem(Vector3 targetPos)
    {
        if (wanderCoroutine != null) StopCoroutine(wanderCoroutine);
        
        currentState = FoxState.GuidingToItem;
        currentItemPosition = targetPos;
        
        navMeshAgent.stoppingDistance = itemStoppingDistance;
        navMeshAgent.SetDestination(targetPos);
    }

    // --- VISUAL FEEDBACK ---

    private void HighlightItem(GameObject item)
    {
        // Add a glowing effect or particle system to the item
        // Example: Enable a child particle system or material glow
        
        // You could also trigger an event for your UI system:
        EventManager.TriggerEvent("ON_FOX_MARKED_ITEM", item);
    }

    private void UnhighlightItem(GameObject item)
    {
        if (item != null)
        {
            // Remove highlight effect
            EventManager.TriggerEvent("ON_FOX_UNMARKED_ITEM", item);
        }
    }

    // --- AUDIO ---

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // --- EVENT LISTENERS ---

    private void OnHintStart(object[] data)
    {
        if (data.Length > 0 && data[0] is Vector3 itemPos)
        {
            GoToItem(itemPos);
        }
    }

    private void OnHintEnd(object[] data)
    {
        if (currentState == FoxState.GuidingToItem)
        {
            StartWandering();
        }
    }

    private void OnItemCollected(object[] data)
    {
        // If the collected item is what we're showing, stop showing it
        if (data.Length > 0 && data[0] is GameObject collectedItem)
        {
            if (currentTargetItem == collectedItem)
            {
                currentTargetItem = null;
                if (currentState == FoxState.ShowingItem || currentState == FoxState.InvestigatingItem)
                {
                    // Celebrate briefly!
                    if (animator != null)
                    {
                        animator.SetTrigger("Celebrate");
                    }
                    StartCoroutine(CelebrateAndReturn());
                }
            }
        }
        else
        {
            // Generic collection - if we were guiding, reset
            if (currentState == FoxState.GuidingToItem || currentState == FoxState.ShowingItem)
            {
                StartWandering();
            }
        }
    }

    private IEnumerator CelebrateAndReturn()
    {
        yield return new WaitForSeconds(1.5f);
        StartWandering();
    }

    // --- UTILITIES ---

    private static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);
        return navHit.position;
    }

    // --- GIZMOS FOR DEBUGGING ---

    private void OnDrawGizmos()
    {
        if (playerTransform != null)
        {
            // Wander radius around player
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerTransform.position, wanderRadius);
        }

        // Item detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, itemDetectionRadius);

        // Current target item
        if (currentTargetItem != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, currentItemPosition);
            Gizmos.DrawWireSphere(currentItemPosition, 0.5f);
        }

        // Bear detection
        if (bearTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, bearDetectionRadius);
            
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(playerTransform != null ? playerTransform.position : transform.position, bearDangerRadius);
        }
    }
}