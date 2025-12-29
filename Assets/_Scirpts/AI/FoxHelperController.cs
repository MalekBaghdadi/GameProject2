using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class FoxHelperController : MonoBehaviour, ISaveable
{
    // ================= CONFIG =================

    [Header("Core References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform cabin;

    [Tooltip("Assign ALL collectible item transforms here")]
    [SerializeField] private List<Transform> itemTargets = new();

    [Header("Movement")]
    [SerializeField] private float guidingRadius = 3f;
    [SerializeField] private float maxLeadDistance = 6f;
    [SerializeField] private float followSpeed = 4f;
    [SerializeField] private float rotationSpeed = 8f;

    [Header("Arrival Thresholds")]
    [SerializeField] private float reachedCabinDistance = 2f;

    // ================= AUDIO =================

    [Header("Audio")]
    [SerializeField] private List<AudioClip> itemFoundBarks = new();
    [SerializeField] private float barkIntervalMin = 10f;
    [SerializeField] private float barkIntervalMax = 15f;
    [SerializeField] private float pitchMin = 0.95f;
    [SerializeField] private float pitchMax = 1.1f;

    // ================= COMPONENTS =================

    private NavMeshAgent agent;
    private Animator animator;
    private AudioSource audioSource;

    // ================= STATE =================

    private Queue<Transform> itemQueue;
    private Transform currentTarget;

    private enum FoxState
    {
        GuidingToItem,
        GuidingToCabin,
        Idle
    }

    private FoxState state = FoxState.Idle;
    private Coroutine barkLoop;
    private bool gameplayStarted;

    // ================= UNITY =================

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        agent.updateRotation = false;
        agent.stoppingDistance = 0.2f;
    }

    private void OnEnable()
    {
        EventManager.Subscribe(EventManager.ON_ITEM_COLLECTED, OnItemCollected);
    }

    private void OnDisable()
    {
        EventManager.Unsubscribe(EventManager.ON_ITEM_COLLECTED, OnItemCollected);
    }

    private void Start()
    {
        agent.speed = followSpeed;
        BuildItemQueue();
        AssignNextItem();

        gameplayStarted = true;
        barkLoop = StartCoroutine(BarkLoop());
    }

    private void Update()
    {
        animator.SetFloat("Speed", agent.velocity.magnitude);

        if (state == FoxState.Idle || currentTarget == null)
            return;

        UpdateGuidingPosition();
        UpdateRotation();
        CheckArrival();
    }

    // ================= GUIDING =================

    private void UpdateGuidingPosition()
    {
        Vector3 toTarget = currentTarget.position - player.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.1f)
            return;

        Vector3 leadDir = toTarget.normalized;

        float playerToFox = Vector3.Distance(player.position, transform.position);
        float leadDistance = Mathf.Clamp(guidingRadius, guidingRadius, maxLeadDistance);

        Vector3 desiredPos = player.position + leadDir * leadDistance;

        if (NavMesh.SamplePosition(desiredPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // fallback: go directly toward target
            agent.SetDestination(currentTarget.position);
        }
    }

    private void UpdateRotation()
    {
        if (agent.velocity.sqrMagnitude < 0.05f)
            return;

        Quaternion rot = Quaternion.LookRotation(agent.velocity.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * rotationSpeed);
    }

    private void CheckArrival()
    {
        float distance = Vector3.Distance(transform.position, currentTarget.position);

        if (state == FoxState.GuidingToCabin && distance <= reachedCabinDistance)
        {
            AssignNextItem();
        }
    }

    // ================= FLOW =================

    private void BuildItemQueue()
    {
        List<Transform> temp = new(itemTargets);

        for (int i = 0; i < temp.Count; i++)
        {
            int r = Random.Range(i, temp.Count);
            (temp[i], temp[r]) = (temp[r], temp[i]);
        }

        itemQueue = new Queue<Transform>(temp);
    }

    private void AssignNextItem()
    {
        if (itemQueue.Count == 0)
        {
            state = FoxState.Idle;
            currentTarget = null;
            return;
        }

        currentTarget = itemQueue.Dequeue();
        state = FoxState.GuidingToItem;
        animator.SetTrigger("FoundItem");
    }

    private void GuideToCabin()
    {
        currentTarget = cabin;
        state = FoxState.GuidingToCabin;
    }

    private void OnItemCollected(object[] _)
    {
        if (state == FoxState.GuidingToItem)
            GuideToCabin();
    }

    // ================= AUDIO =================

    private IEnumerator BarkLoop()
    {
        while (!gameplayStarted)
            yield return null;

        while (true)
        {
            yield return new WaitForSeconds(Random.Range(barkIntervalMin, barkIntervalMax));

            if (state != FoxState.Idle && itemFoundBarks.Count > 0)
            {
                audioSource.pitch = Random.Range(pitchMin, pitchMax);
                audioSource.PlayOneShot(itemFoundBarks[Random.Range(0, itemFoundBarks.Count)]);
                audioSource.pitch = 1f;
            }
        }
    }

    // ================= SAVE =================

    public void SaveData(ref GameData data)
    {
        data.foxPosition = transform.position;
    }

    public void LoadData(GameData data)
    {
        agent.Warp(data.foxPosition);
        BuildItemQueue();
        AssignNextItem();
    }
}
