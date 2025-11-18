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

    enum State { Searching, Chasing, Attacking }
    State state = State.Searching;

    // wandering
    Vector3 wanderTarget;
    float nextWanderTimeLocal = 0f;
    float wanderIntervalLocal = 2f;
    float wanderRadiusLocal = 8f;

    // interest / audio
    float interest = 0f; // 0 .. data.maxInterest

    // losing sight
    float timeSinceLastSeen = Mathf.Infinity;
    public float memoryTime = 1f; // seconds to keep chasing after losing sight

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

        bool seen = CheckSight();
        bool heard = CheckAudioDistance();

        switch (state)
        {
            case State.Searching:
                SearchingUpdate(seen || heard, heard);
                break;
            case State.Chasing:
                ChasingUpdate(seen);
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
    void SearchingUpdate(bool detected, bool heard)
    {
        // If we truly see the player, immediate chase
        if (detected && CheckSight())
        {
            state = State.Chasing;
            agent.speed = data.chaseSpeed;
            timeSinceLastSeen = 0f;
            return;
        }

        // If heard but not seen, rotate and increase interest
        if (heard && !CheckSight())
        {
            Vector3 dir = (player.position - transform.position);
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 4f);

            interest = Mathf.Min(data.maxInterest, interest + data.interestIncreaseRate * Time.deltaTime * 0.6f);
        }
        else
        {
            interest = Mathf.Max(0f, interest - data.interestDecayRate * Time.deltaTime);
        }

        if (Time.time >= nextWanderTimeLocal || Vector3.Distance(transform.position, wanderTarget) < 1f)
        {
            ChooseNewWanderTarget();
            nextWanderTimeLocal = Time.time + wanderIntervalLocal;
        }

        Vector3 biasedTarget = Vector3.Lerp(wanderTarget, player.position, interest);
        agent.SetDestination(biasedTarget);
    }

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

    #region Chasing
    void ChasingUpdate(bool seen)
    {
        if (seen)
        {
            timeSinceLastSeen = 0f;
        }
        else
        {
            timeSinceLastSeen += Time.deltaTime;
            if (timeSinceLastSeen >= memoryTime)
            {
                // forgot player — return to searching
                state = State.Searching;
                agent.speed = data.wanderSpeed;
                return;
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

        // wind-up time before applying damage (tweak if you use animation events)
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

        // finish attack animation time if needed
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

        Vector3 toPlayer = player.position - transform.position;
        float dist = toPlayer.magnitude;
        if (dist > data.sightRadius) return false;

        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > data.viewAngle * 0.5f) return false;

        Vector3 origin = transform.position + Vector3.up * 1f;
        Vector3 dir = (player.position + Vector3.up * 0.9f - origin).normalized;
        RaycastHit hit;
        if (Physics.Raycast(origin, dir, out hit, data.sightRadius))
        {
            if (hit.collider.CompareTag("Player"))
            {
                interest = Mathf.Min(data.maxInterest, interest + data.interestIncreaseRate * Time.deltaTime * 2.0f);
                return true;
            }
        }

        return false;
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

            interest = Mathf.Min(data.maxInterest, interest + data.interestIncreaseRate * loudness);
        }
    }
    #endregion

    #region Gizmos
    void OnDrawGizmosSelected()
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

        // Raycast line (play mode)
        if (Application.isPlaying && player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin + Vector3.up, player.position + Vector3.up);
        }
    }
    #endregion
}
