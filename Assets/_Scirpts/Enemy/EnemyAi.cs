using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour, ISaveable
{
    public EnemyData data; // assign in inspector (tunable values)
    
    private MimicSpace.Mimic mimicScript;

    NavMeshAgent agent;
    Transform player;
    Animator anim; // optional, if you have an Animator

    enum State { Searching, Chasing, MovingToLastKnown, LocalSearch, Attacking }
    State state = State.Searching;

    // wandering
    Vector3 wanderTarget;
    float nextWanderTimeLocal = 0f;
    float wanderIntervalLocal = 2f;
    float wanderRadiusLocal = 8f;

    // interest / audio
    float interest = 0f; // 0 .. data.maxInterest

    // automatic interest settings (editable in Inspector)
    [Header("Auto-Interest (idle suspicion)")]
    [Tooltip("Seconds to wait while not seeing/hearing player before interest auto-starts increasing.")]
    [SerializeField] private float minTimeBeforeAutoInterest = 5f;
    [Tooltip("How fast interest increases per second automatically (when idle).")]
    [SerializeField] private float autoInterestRate = 0.15f;
    [Tooltip("Maximum interest value produced by automatic increase (will also be clamped by data.maxInterest).")]
    [SerializeField] private float autoInterestMax = 0.5f;

    // idle timer to track time since last detection/hearing
    float idleTimer = 0f;

    // losing sight
    float timeSinceLastSeen = Mathf.Infinity;
    public float memoryTime = 1f; // seconds to keep chasing after losing sight

    // Last known position / local search
    [Header("Last Known Position / Local Search")]
    [Tooltip("How long the enemy will search around the last known position before giving up.")]
    public float searchDuration = 4f;
    [Tooltip("Radius for local wandering while searching the last known position.")]
    public float searchRadius = 4f;
    [Tooltip("How close the enemy must get to the last known position to start local searching.")]
    public float lastKnownArrivalThreshold = 1.2f;

    Vector3 lastKnownPosition;
    bool hasLastKnown = false;
    float localSearchTimer = 0f;

    // attacking
    bool canAttack = true;
    bool isAttacking = false;
    
    
    [Header("Proximity Audio")]
    [Tooltip("List of sounds to play when the enemy is close to the player. Will cycle through them.")]
    [SerializeField] private AudioClip[] proximityClips; 
    [Tooltip("Distance (meters) at which the proximity sound will play.")]
    [SerializeField] private float proximityDistance = 6f;
    [Tooltip("If player stays within proximity, repeat the sound every this many seconds (0 = play only once on enter).")]
    [SerializeField] private float proximityRepeatInterval = 2f;

    private AudioSource proximitySource;
    private float proximityTimer = 0f;
    private bool playerWasInRange = false;

    [Header("Attack Audio")]
    [Tooltip("Sound to play when this enemy performs an attack (played at wind-up/hit).")]
    [SerializeField] private AudioClip attackClip;
    [Tooltip("Base pitch to apply to the attack sound.")]
    [SerializeField] private float attackPitch = 1f;
    [Tooltip("Random pitch variance applied every time (±).")]
    [SerializeField] private float attackPitchVariance = 0.0f;

    private AudioSource attackSource;

    // round-robin index for cycling through proximityClips
    private int nextProximityIndex = 0;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        // Get the player position safely
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        mimicScript = GetComponent<MimicSpace.Mimic>();
        
        // create/get audio source used for proximity sounds
        proximitySource = GetComponent<AudioSource>();
        if (proximitySource == null)
        {
            proximitySource = gameObject.AddComponent<AudioSource>();
            proximitySource.playOnAwake = false;
            proximitySource.spatialBlend = 1f; // 3D sound
            proximitySource.rolloffMode = AudioRolloffMode.Linear;
            proximitySource.maxDistance = Mathf.Max(10f, proximityDistance * 2f);
        }
        
        // create a dedicated audio source for attack sounds (separate so we can set pitch independently)
        attackSource = gameObject.AddComponent<AudioSource>();
        attackSource.playOnAwake = false;
        attackSource.spatialBlend = 1f; // 3D sound
        attackSource.rolloffMode = AudioRolloffMode.Linear;
        attackSource.maxDistance = Mathf.Max(10f, proximityDistance * 2f);

        // ensure index is valid
        nextProximityIndex = 0;
        
        if (data != null)
            agent.speed = data.wanderSpeed;

        ChooseNewWanderTarget();
    }

    void Update()
    {
        if (player == null) return;
        if (data == null) return;

        bool seen = CheckSight();             // distance & angle detection (interest updated here)
        bool heard = CheckAudioDistance();
        
        if (mimicScript != null && agent != null)
        {
            // We pass the agent's desired velocity or actual velocity
            mimicScript.velocity = agent.velocity;
        }
        
        // --- Proximity sound handling ---
        // --- Proximity sound handling (only when game actually started) ---
        if (!GameState.IsGameStarted)
        {
            // Ensure proximity state is reset while still in the menu/paused so it doesn't immediately trigger on start.
            playerWasInRange = false;
            proximityTimer = 0f;
        }
        else
        {
            float distToPlayer = Vector3.Distance(transform.position, player.position);

            if (distToPlayer <= proximityDistance)
            {
                if (!playerWasInRange)
                {
                    // Player just entered range
                    playerWasInRange = true;
                    // allow immediate play on enter (unless repeat interval is > 0 and we want to wait)
                    proximityTimer = 0f;
                }

                if (proximityRepeatInterval <= 0f)
                {
                    // play only on initial enter (guard against multiple frames)
                    if (proximityTimer == 0f)
                    {
                        PlayNextProximityClip();
                    }
                    // set to -1 to indicate we've played the one-shot for this stay-in-range
                    proximityTimer = -1f;
                }
                else
                {
                    proximityTimer -= Time.deltaTime;
                    if (proximityTimer <= 0f)
                    {
                        PlayNextProximityClip();
                        proximityTimer = proximityRepeatInterval;
                    }
                }
            }
            else
            {
                // player left range -> reset so it will re-trigger on next enter
                if (playerWasInRange)
                {
                    playerWasInRange = false;
                    proximityTimer = 0f;
                }
            }
        }



        switch (state)
        {
            case State.Searching:
                SearchingUpdate(seen, heard);
                break;
            case State.Chasing:
                ChasingUpdate(seen);
                break;
            case State.MovingToLastKnown:
                MoveToLastKnownUpdate(seen);
                break;
            case State.LocalSearch:
                LocalSearchUpdate(seen);
                break;
            case State.Attacking:
                AttackingUpdate();
                break;
        }

        // optional animator speed param
        //if (anim != null)
         //   anim.SetFloat("Speed", agent.velocity.magnitude);
    }
    
    // helper: choose next clip and play it (round-robin)
    void PlayNextProximityClip()
    {
        if (proximitySource == null || proximityClips == null || proximityClips.Length == 0) return;

        AudioClip clip = proximityClips[nextProximityIndex];
        if (clip != null)
        {
            proximitySource.PlayOneShot(clip);
        }
        nextProximityIndex = (nextProximityIndex + 1) % proximityClips.Length;
    }


    #region Searching
    void SearchingUpdate(bool seen, bool heard)
    {
        // If we truly see the player, immediate chase
        if (seen)
        {
            // update last known as we see the player
            UpdateLastKnown(player.position);
            state = State.Chasing;
            agent.speed = data.chaseSpeed;
            timeSinceLastSeen = 0f;
            idleTimer = 0f; // reset idle since we saw player
            return;
        }

        // If heard but not seen, rotate and increase interest (stronger than auto)
        if (heard)
        {
            idleTimer = 0f; // reset idle because audio is interaction
            Vector3 dir = (player.position - transform.position);
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 4f);

            interest = Mathf.Min(data.maxInterest, interest + data.interestIncreaseRate * Time.deltaTime * 0.6f);

            // also update last known when hearing (optionally) — uncomment if you want:
            // UpdateLastKnown(player.position);
        }
        else
        {
            // not heard & not seen -> we are idle. increment idleTimer and possibly auto-increase interest.
            idleTimer += Time.deltaTime;

            // automatic interest only starts after minTimeBeforeAutoInterest
            if (idleTimer >= minTimeBeforeAutoInterest)
            {
                // grow interest automatically up to autoInterestMax (but never exceed data.maxInterest)
                float autoCap = Mathf.Min(autoInterestMax, data.maxInterest);
                interest = Mathf.Min(autoCap, interest + autoInterestRate * Time.deltaTime);
            }
            else
            {
                // normal decay before auto-interest kicks in
                interest = Mathf.Max(0f, interest - data.interestDecayRate * Time.deltaTime);
            }
        }

        // Wander target selection
        if (Time.time >= nextWanderTimeLocal || Vector3.Distance(transform.position, wanderTarget) < 1f)
        {
            ChooseNewWanderTarget();
            nextWanderTimeLocal = Time.time + wanderIntervalLocal;
        }

        // Bias wander toward player by current interest (which may be raised via auto-interest)
        Vector3 biasedTarget = Vector3.Lerp(wanderTarget, player.position, interest);
        agent.SetDestination(biasedTarget);
    }
    #endregion

    #region Chasing
    void ChasingUpdate(bool seen)
    {
        if (seen)
        {
            timeSinceLastSeen = 0f;
            idleTimer = 0f; // reset idle
            // update last known while we still see player
            UpdateLastKnown(player.position);
        }
        else
        {
            timeSinceLastSeen += Time.deltaTime;
            // if we lose sight but have a last known position, go to it
            if (timeSinceLastSeen >= 0.05f) // small buffer: immediately react
            {
                if (hasLastKnown)
                {
                    state = State.MovingToLastKnown;
                    agent.speed = data.chaseSpeed;
                    agent.SetDestination(lastKnownPosition);
                    return;
                }
                else if (timeSinceLastSeen >= memoryTime)
                {
                    // no last known available, fallback to wandering
                    state = State.Searching;
                    agent.speed = data.wanderSpeed;
                    return;
                }
            }
        }

        // Move toward player
        agent.SetDestination(player.position);

        // If within attack range -> attack
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= data.attackRange)
        {
            // stop agent path and switch to attack
            agent.ResetPath();
            state = State.Attacking;
            isAttacking = false; // reset
        }
    }
    #endregion

    #region MoveToLastKnown / LocalSearch
    void MoveToLastKnownUpdate(bool seen)
    {
        // If we see player again while moving to last known, switch to chasing
        if (seen)
        {
            state = State.Chasing;
            agent.speed = data.chaseSpeed;
            UpdateLastKnown(player.position);
            return;
        }

        // If we reached the last known position (or very near), start local search
        float dist = Vector3.Distance(transform.position, lastKnownPosition);
        if (dist <= lastKnownArrivalThreshold)
        {
            StartLocalSearch();
            return;
        }

        // If agent somehow lost path, try to set destination again
        if (!agent.hasPath)
            agent.SetDestination(lastKnownPosition);
    }

    void StartLocalSearch()
    {
        state = State.LocalSearch;
        localSearchTimer = searchDuration;
        // pick an initial local wander target near the last known position
        ChooseLocalSearchTarget();
    }

    void LocalSearchUpdate(bool seen)
    {
        // If we see player while searching locally -> chase again
        if (seen)
        {
            state = State.Chasing;
            agent.speed = data.chaseSpeed;
            UpdateLastKnown(player.position);
            return;
        }

        // While searching locally, pick random nearby points to investigate
        if (localSearchTimer > 0f)
        {
            localSearchTimer -= Time.deltaTime;

            // If we reached current local target or we don't have one, choose a new one
            if (!agent.hasPath || Vector3.Distance(transform.position, agent.destination) < 0.9f)
            {
                ChooseLocalSearchTarget();
            }

            // Optionally rotate slowly to look around
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, transform.eulerAngles.y + 120f * Time.deltaTime, 0f), Time.deltaTime * 1.0f);
        }
        else
        {
            // local search finished, clear last known and return to searching (wandering)
            ClearLastKnown();
            state = State.Searching;
            agent.speed = data.wanderSpeed;
            ChooseNewWanderTarget();
        }
    }

    void ChooseLocalSearchTarget()
    {
        Vector3 randomDir = Random.insideUnitSphere * searchRadius + lastKnownPosition;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDir, out hit, searchRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // fallback: stay at last known
            agent.SetDestination(lastKnownPosition);
        }
    }
    #endregion

    #region Attacking
    void AttackingUpdate()
    {
        // Face the player smoothly
        Vector3 look = player.position - transform.position;
        look.y = 0;
        if (look.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), Time.deltaTime * 8f);

        float dist = Vector3.Distance(transform.position, player.position);

        // If player walked away beyond attackRange + small buffer, go back to chase
        if (dist > data.attackRange + 0.35f)
        {
            state = State.Chasing;
            agent.speed = data.chaseSpeed;
            return;
        }

        // If we can attack, do it
        if (canAttack && !isAttacking)
        {
            StartCoroutine(DoAttack());
        }
    }

    IEnumerator DoAttack()
    {
        isAttacking = true;
        canAttack = false;

        // trigger animation if exists
        if (anim != null)
        {
            anim.SetTrigger("Attack");
        }

        // wind-up time before applying damage
        float windup = 0.25f;
        yield return new WaitForSeconds(windup);
        
        // play attack sound at wind-up (so player hears it before damage)
        if (GameState.IsGameStarted && attackClip != null && attackSource != null)
        {
            attackSource.pitch = attackPitch + Random.Range(-attackPitchVariance, attackPitchVariance);
            attackSource.PlayOneShot(attackClip);
        }


        // Apply damage if still in range
        if (Vector3.Distance(transform.position, player.position) <= data.attackRange + 0.25f)
        {
            // --- MODIFIED SECTION ---
            // Instead of looking for PlayerHealth, we trigger the EventManager event
            // that the PlayerController is listening for.
            // Payload: [0] Attacker Transform, [1] Damage Amount
            EventManager.TriggerEvent(EventManager.ON_BEAR_ATTACK, transform, data.damage);
        }

        // finish attack animation time
        float postDelay = 0.05f;
        yield return new WaitForSeconds(postDelay);

        // cooldown before next attack
        yield return new WaitForSeconds(data.attackCooldown);

        canAttack = true;
        isAttacking = false;
    }
    #endregion

    #region Detection
    bool CheckSight()
    {
        if (player == null || data == null) return false;

        // distance check
        Vector3 toPlayer = player.position - transform.position;
        float dist = toPlayer.magnitude;
        if (dist > data.sightRadius) return false;

        // angle check (no raycast LOS)
        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > data.viewAngle * 0.5f) return false;

        // Player considered "seen" (no obstruction check)
        interest = Mathf.Min(data.maxInterest, interest + data.interestIncreaseRate * Time.deltaTime * 2.0f);

        // update last known immediately when seen
        UpdateLastKnown(player.position);

        return true;
    }

    bool CheckAudioDistance()
    {
        if (player == null || data == null) return false;
        float d = Vector3.Distance(transform.position, player.position);
        return d <= data.audioRadius;
    }

    public void OnPlayerMadeNoise(Vector3 noisePos, float loudness)
    {
        if (data == null) return;

        float dist = Vector3.Distance(transform.position, noisePos);
        if (dist <= data.audioRadius * loudness)
        {
            Vector3 dir = (noisePos - transform.position);
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8f);

            // hearing is a stronger/interactable boost — reset idle timer
            idleTimer = 0f;
            interest = Mathf.Min(data.maxInterest, interest + data.interestIncreaseRate * loudness);

            // optionally update last known when hearing
            UpdateLastKnown(noisePos);
        }
    }
    #endregion

    #region Last Known helpers
    void UpdateLastKnown(Vector3 pos)
    {
        lastKnownPosition = pos;
        hasLastKnown = true;
    }

    void ClearLastKnown()
    {
        hasLastKnown = false;
        lastKnownPosition = Vector3.zero;
    }
    #endregion

    #region Wandering helpers
    void ChooseNewWanderTarget()
    {
        Vector3 randomDir = Random.insideUnitSphere * wanderRadiusLocal + transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDir, out hit, wanderRadiusLocal, NavMesh.AllAreas))
        {
            wanderTarget = hit.position;
        }
        else
        {
            wanderTarget = transform.position;
        }
    }
    #endregion
    
    public void SaveData(ref GameData data)
    {
        data.enemyPosition = transform.position;
    }

    // In EnemyAI.cs

    public void LoadData(GameData data)
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false; 
            
            transform.position = data.enemyPosition;
            agent.enabled = true;
        }
        else
        {
            // Fallback if NavMeshAgent component is missing
            transform.position = data.enemyPosition;
        }
    }

    #region Gizmos
    void OnDrawGizmos()
    {
        if (data == null) return;

        // Sight radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, data.sightRadius);

        // View angle cone
        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        float halfAngle = data.viewAngle * 0.5f;
        float radius = data.sightRadius;

        Quaternion leftRot = Quaternion.Euler(0, -halfAngle, 0);
        Vector3 leftDir = leftRot * forward;
        Quaternion rightRot = Quaternion.Euler(0, halfAngle, 0);
        Vector3 rightDir = rightRot * forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin, origin + leftDir * radius);
        Gizmos.DrawLine(origin, origin + rightDir * radius);

        int segments = 30;
        Vector3 prev = origin + leftDir * radius;
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Quaternion rot = Quaternion.Euler(0, angle, 0);
            Vector3 dir = rot * forward;
            Vector3 next = origin + dir * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }

        // Audio radius
        Gizmos.color = new Color(0.5f, 1f, 0.5f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, data.audioRadius);
        
        // Proximity sphere gizmo (customizable)
        Gizmos.color = new Color(1f, 0.2f, 0.8f, 0.12f); // translucent magenta-ish
        Gizmos.DrawWireSphere(transform.position, proximityDistance);

        // Attack range (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, data.attackRange);

        // Last known position (blue)
        if (hasLastKnown)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(lastKnownPosition, 0.25f);
            Gizmos.DrawWireSphere(lastKnownPosition, searchRadius);
        }
    }
    #endregion
}