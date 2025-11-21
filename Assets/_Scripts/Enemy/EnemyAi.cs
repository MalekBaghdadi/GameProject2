using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public EnemyData data; // assign in inspector (tunable values)

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

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

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
        if (anim != null)
            anim.SetFloat("Speed", agent.velocity.magnitude);
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

        // Apply damage if still in range
        if (Vector3.Distance(transform.position, player.position) <= data.attackRange + 0.25f)
        {
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(data.damage);
            }
            else
            {
                Debug.LogWarning("PlayerHealth component missing on Player.");
            }
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

        // Raycast line (play mode)
        if (Application.isPlaying && player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin + Vector3.up, player.position + Vector3.up);
        }
    }
    #endregion
}
